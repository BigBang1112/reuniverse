namespace Reuniverse.Razor.Tests;

/// <summary>
/// Serves ranges of the data like the browser would, and records what was asked for.
/// </summary>
internal sealed class FakeFileReader(byte[] data) : IReuFileReader
{
    public List<(long Offset, int Count)> Reads { get; } = [];

    /// <summary>
    /// Limits how many bytes a single read returns, to act like a client that returns less than asked for.
    /// </summary>
    public int? MaxReadSize { get; set; }

    /// <summary>
    /// Pretends the file got shorter after it was selected.
    /// </summary>
    public long? AvailableLength { get; set; }

    public bool Disposed { get; private set; }

    public static byte[] CreateData(int length) => Enumerable.Range(0, length).Select(i => (byte)(i % 251)).ToArray();

    public ValueTask<int> ReadAsync(long offset, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        Reads.Add((offset, buffer.Length));

        var available = AvailableLength ?? data.Length;
        var count = (int)Math.Clamp(available - offset, 0, Math.Min(buffer.Length, MaxReadSize ?? int.MaxValue));

        data.AsSpan((int)offset, count).CopyTo(buffer.Span);

        return ValueTask.FromResult(count);
    }

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }
}
