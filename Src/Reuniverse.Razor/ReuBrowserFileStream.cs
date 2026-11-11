using Microsoft.JSInterop;

namespace Reuniverse.Razor;

/// <summary>
/// A read-only, seekable stream over a file that stays on the client. Only the requested ranges are transferred,
/// so files of any size can be read without loading them into memory.
/// </summary>
/// <remarks>
/// <para>
/// Reads are served from a small cache of blocks (<see cref="BlockSize"/> each, at most <see cref="MaxCachedBlocks"/> at once).
/// Reads at least as large as a block skip the cache.
/// </para>
/// <para>
/// Browsers only read files asynchronously, so <b>synchronous reads only work for data that is already in memory</b>:
/// the cached blocks and the range loaded by <see cref="PrefetchAsync"/>. Other synchronous reads throw <see cref="NotSupportedException"/>.
/// Prefetch the range before handing the stream to code that reads synchronously.
/// </para>
/// <para>The stream is not thread-safe. Dispose it to release the file on the client.</para>
/// </remarks>
public sealed class ReuBrowserFileStream : Stream
{
    /// <summary>
    /// The default size of a cached block, 1 MiB.
    /// </summary>
    public const int DefaultBlockSize = 1024 * 1024;

    /// <summary>
    /// The number of blocks kept in the cache.
    /// </summary>
    public const int MaxCachedBlocks = 4;

    private readonly IReuFileReader reader;
    private readonly long length;

    // most recently used first
    private readonly LinkedList<Block> blocks = new();
    private Block? prefetched;

    private long position;
    private bool disposed;

    internal ReuBrowserFileStream(IReuFileReader reader, long length, int blockSize = DefaultBlockSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(blockSize);

        this.reader = reader;
        this.length = length;
        BlockSize = blockSize;
    }

    /// <summary>
    /// Creates a stream over the file object created by the upload script.
    /// </summary>
    internal static ReuBrowserFileStream Create(IJSObjectReference file, long length)
    {
        return new ReuBrowserFileStream(new JSFileReader(file, useStreamReference: !OperatingSystem.IsBrowser()), length);
    }

    /// <summary>
    /// The size of a cached block in bytes.
    /// </summary>
    public int BlockSize { get; }

    public override bool CanRead => !disposed;
    public override bool CanSeek => !disposed;
    public override bool CanWrite => false;

    public override long Length
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return length;
        }
    }

    public override long Position
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return position;
        }
        set
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            position = value;
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        var newPosition = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => position + offset,
            SeekOrigin.End => length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };

        if (newPosition < 0)
        {
            throw new IOException("An attempt was made to move the position before the beginning of the stream.");
        }

        position = newPosition;
        return position;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        var count = GetReadableCount(buffer.Length);

        if (count == 0)
        {
            return 0;
        }

        buffer = buffer[..count];

        if (TryReadCached(buffer.Span, out var read))
        {
            return read;
        }

        if (count >= BlockSize)
        {
            read = await ReadSourceAsync(position, buffer, cancellationToken);
            position += read;
            return read;
        }

        var block = await LoadBlockAsync(position / BlockSize * BlockSize, cancellationToken);
        return CopyFrom(block, buffer.Span);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateBufferArguments(buffer, offset, count);
        return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    public override int Read(Span<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        var count = GetReadableCount(buffer.Length);

        if (count == 0)
        {
            return 0;
        }

        if (TryReadCached(buffer[..count], out var read))
        {
            return read;
        }

        throw new NotSupportedException(
            $"Synchronous reads of a browser file only work for data that is already in memory, but position {position} is not. " +
            $"Use ReadAsync, or call {nameof(PrefetchAsync)} for the range before reading it synchronously.");
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        return Read(buffer.AsSpan(offset, count));
    }

    /// <summary>
    /// Loads the range into memory, so that it can also be read synchronously. Replaces the previously prefetched range.
    /// The range is clamped to the end of the file, and a <paramref name="count"/> of 0 releases the prefetched range.
    /// </summary>
    /// <param name="offset">The position where the range starts.</param>
    /// <param name="count">The length of the range in bytes. The whole range is held in memory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task PrefetchAsync(long offset, int count, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        count = (int)Math.Clamp(length - offset, 0, count);

        if (count == 0)
        {
            prefetched = null;
            return;
        }

        if (prefetched is not null && prefetched.Start <= offset && offset + count <= prefetched.End)
        {
            return;
        }

        // the previous range is released first, so both are not held at once
        prefetched = null;
        prefetched = new Block(offset, await ReadExactlyFromSourceAsync(offset, count, cancellationToken));
    }

    private int GetReadableCount(int requested)
    {
        return position >= length ? 0 : (int)Math.Min(requested, length - position);
    }

    private bool TryReadCached(Span<byte> buffer, out int read)
    {
        if (prefetched?.Contains(position) == true)
        {
            read = CopyFrom(prefetched, buffer);
            return true;
        }

        for (var node = blocks.First; node is not null; node = node.Next)
        {
            if (node.Value.Contains(position))
            {
                blocks.Remove(node);
                blocks.AddFirst(node);

                read = CopyFrom(node.Value, buffer);
                return true;
            }
        }

        read = 0;
        return false;
    }

    private int CopyFrom(Block block, Span<byte> buffer)
    {
        var offset = (int)(position - block.Start);
        var count = Math.Min(buffer.Length, block.Data.Length - offset);

        block.Data.AsSpan(offset, count).CopyTo(buffer);
        position += count;

        return count;
    }

    private async Task<Block> LoadBlockAsync(long start, CancellationToken cancellationToken)
    {
        var size = (int)Math.Min(BlockSize, length - start);
        var block = new Block(start, await ReadExactlyFromSourceAsync(start, size, cancellationToken));

        blocks.AddFirst(block);

        if (blocks.Count > MaxCachedBlocks)
        {
            blocks.RemoveLast();
        }

        return block;
    }

    private async Task<byte[]> ReadExactlyFromSourceAsync(long offset, int count, CancellationToken cancellationToken)
    {
        var data = new byte[count];
        var total = 0;

        while (total < count)
        {
            total += await ReadSourceAsync(offset + total, data.AsMemory(total), cancellationToken);
        }

        return data;
    }

    private async Task<int> ReadSourceAsync(long offset, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var read = await reader.ReadAsync(offset, buffer, cancellationToken);

        if (read <= 0)
        {
            throw new IOException("The file ended before its reported size. It may have changed since it was selected.");
        }

        return Math.Min(read, buffer.Length);
    }

    public override void Flush()
    {
    }

    public override void SetLength(long value) => throw new NotSupportedException("The stream is read-only.");

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException("The stream is read-only.");

    protected override void Dispose(bool disposing)
    {
        if (disposing && !disposed)
        {
            ReleaseCache();

            // releasing the file on the client is asynchronous, and failing to do so is not actionable
            _ = DisposeReaderAsync(reader);
        }

        base.Dispose(disposing);

        static async Task DisposeReaderAsync(IReuFileReader reader)
        {
            try
            {
                await reader.DisposeAsync();
            }
            catch
            {
            }
        }
    }

    public override async ValueTask DisposeAsync()
    {
        if (!disposed)
        {
            ReleaseCache();

            try
            {
                await reader.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        GC.SuppressFinalize(this);
    }

    private void ReleaseCache()
    {
        disposed = true;
        blocks.Clear();
        prefetched = null;
    }

    private sealed record Block(long Start, byte[] Data)
    {
        public long End => Start + Data.Length;

        public bool Contains(long position) => position >= Start && position < End;
    }
}
