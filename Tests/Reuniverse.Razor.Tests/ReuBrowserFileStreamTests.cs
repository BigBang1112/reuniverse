namespace Reuniverse.Razor.Tests;

public class ReuBrowserFileStreamTests
{
    private const int BlockSize = 16;
    private const int FileSize = 100;

    private readonly byte[] data = FakeFileReader.CreateData(FileSize);
    private readonly FakeFileReader reader;
    private readonly ReuBrowserFileStream stream;

    public ReuBrowserFileStreamTests()
    {
        reader = new FakeFileReader(data);
        stream = new ReuBrowserFileStream(reader, FileSize, BlockSize);
    }

    [After(Test)]
    public async Task CleanUpAsync()
    {
        await stream.DisposeAsync();
        await reader.DisposeAsync();
    }

    private async Task<byte[]> ReadAsync(int count)
    {
        var buffer = new byte[count];
        await stream.ReadExactlyAsync(buffer);
        return buffer;
    }

    private byte[] Expected(int offset, int count) => data.AsSpan(offset, count).ToArray();

    [Test]
    public async Task CopyToAsync_WholeFile_ReturnsAllData()
    {
        // Arrange
        using var copy = new MemoryStream();

        // Act
        await stream.CopyToAsync(copy);

        // Assert
        await copy.ToArray().ShouldEqualInOrderAsync(data);
    }

    [Test]
    public async Task ReadAsync_AfterSeek_ReturnsDataAtPosition()
    {
        // Arrange
        stream.Seek(40, SeekOrigin.Begin);

        // Act
        var result = await ReadAsync(10);

        // Assert
        await result.ShouldEqualInOrderAsync(Expected(40, 10));
        await Assert.That(stream.Position).IsEqualTo(50);
    }

    [Test]
    public async Task ReadAsync_SmallReadsInOneBlock_ReadsFileOnce()
    {
        // Act
        for (var i = 0; i < BlockSize; i++)
        {
            await ReadAsync(1);
        }

        // Assert
        await reader.Reads.ShouldEqualInOrderAsync((0L, BlockSize));
    }

    [Test]
    public async Task ReadAsync_InsideBlock_ReadsWholeAlignedBlock()
    {
        // Arrange
        stream.Position = 20;

        // Act
        await ReadAsync(2);

        // Assert
        await reader.Reads.ShouldEqualInOrderAsync((16L, BlockSize));
    }

    [Test]
    public async Task ReadAsync_AcrossBlockBoundary_ReturnsPartialCountFromFirstBlock()
    {
        // Arrange
        stream.Position = 14;
        var buffer = new byte[4];

        // Act
        var read = await stream.ReadAsync(buffer);

        // Assert
        await Assert.That(read).IsEqualTo(2);
        await buffer.Take(2).ShouldEqualInOrderAsync(Expected(14, 2));
    }

    [Test]
    public async Task ReadAsync_LastBlock_ReadsOnlyUntilEnd()
    {
        // Arrange
        stream.Position = 98;

        // Act
        var result = await ReadAsync(2);

        // Assert
        await result.ShouldEqualInOrderAsync(Expected(98, 2));
        await reader.Reads.ShouldEqualInOrderAsync((96L, 4));
    }

    [Test]
    public async Task ReadAsync_AtLeastBlockSize_SkipsCache()
    {
        // Arrange
        stream.Position = 5;

        // Act
        var result = await ReadAsync(BlockSize * 2);

        // Assert
        await result.ShouldEqualInOrderAsync(Expected(5, BlockSize * 2));
        await reader.Reads.ShouldEqualInOrderAsync((5L, BlockSize * 2));
    }

    [Test]
    public async Task ReadAsync_MoreBlocksThanCache_RereadsLeastRecentlyUsed()
    {
        // Arrange
        for (var block = 0; block <= ReuBrowserFileStream.MaxCachedBlocks; block++)
        {
            stream.Position = block * BlockSize;
            await ReadAsync(1);
        }

        reader.Reads.Clear();

        // Act
        stream.Position = 0;
        await ReadAsync(1);

        // Assert
        await reader.Reads.ShouldEqualInOrderAsync((0L, BlockSize));
    }

    [Test]
    public async Task ReadAsync_RecentlyUsedBlock_StaysCached()
    {
        // Arrange
        await ReadAsync(1);

        for (var block = 1; block <= ReuBrowserFileStream.MaxCachedBlocks; block++)
        {
            // touching block 0 in between keeps it the most recent
            stream.Position = 0;
            await ReadAsync(1);

            stream.Position = block * BlockSize;
            await ReadAsync(1);
        }

        reader.Reads.Clear();

        // Act
        stream.Position = 0;
        await ReadAsync(1);

        // Assert
        await Assert.That(reader.Reads.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments(FileSize)]
    [Arguments(FileSize + 10)]
    public async Task ReadAsync_AtOrPastEnd_ReturnsZero(long position)
    {
        // Arrange
        stream.Position = position;

        // Act
        var read = await stream.ReadAsync(new byte[4]);

        // Assert
        await Assert.That(read).IsEqualTo(0);
        await Assert.That(reader.Reads.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ReadAsync_WhenClientReturnsLess_KeepsReadingUntilBlockIsFull()
    {
        // Arrange
        reader.MaxReadSize = 5;

        // Act
        await ReadAsync(1);
        var result = await ReadAsync(BlockSize - 1);

        // Assert
        await result.ShouldEqualInOrderAsync(Expected(1, BlockSize - 1));
        await reader.Reads.Select(x => x.Offset).ShouldEqualInOrderAsync(0L, 5L, 10L, 15L);
    }

    [Test]
    public async Task ReadAsync_WhenFileGotShorter_ThrowsIOException()
    {
        // Arrange
        reader.AvailableLength = 50;
        stream.Position = 60;

        // Act
        var act = async () => await stream.ReadAsync(new byte[4]);

        // Assert
        await Assert.That(act).Throws<IOException>();
    }

    [Test]
    public async Task ReadAsync_WithArray_ReadsIntoOffset()
    {
        // Arrange
        var buffer = new byte[6];

        // Act
        var read = await stream.ReadAsync(buffer, 2, 4, CancellationToken.None);

        // Assert
        await Assert.That(read).IsEqualTo(4);
        await buffer.ShouldEqualInOrderAsync<byte>([0, 0, .. Expected(0, 4)]);
    }

    [Test]
    public async Task Read_FromCachedBlock_ReturnsData()
    {
        // Arrange
        await ReadAsync(1);

        // Act
        var buffer = new byte[4];
        var read = stream.Read(buffer, 0, buffer.Length);

        // Assert
        await Assert.That(read).IsEqualTo(4);
        await buffer.ShouldEqualInOrderAsync(Expected(1, 4));
    }

    [Test]
    public async Task Read_NotInMemory_ThrowsNotSupportedException()
    {
        // Act
        var act = () => stream.Read(new byte[4], 0, 4);

        // Assert
        await Assert.That(act).Throws<NotSupportedException>();
    }

    [Test]
    public async Task Read_AtEnd_ReturnsZeroWithoutData()
    {
        // Arrange
        stream.Position = FileSize;

        // Act
        var read = stream.Read(new byte[4], 0, 4);

        // Assert
        await Assert.That(read).IsEqualTo(0);
    }

    [Test]
    public async Task PrefetchAsync_ThenSyncReads_ReadWholeRangeWithoutFile()
    {
        // Arrange
        await stream.PrefetchAsync(30, 50);
        reader.Reads.Clear();
        stream.Position = 30;

        // Act
        using var binaryReader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        var result = binaryReader.ReadBytes(50);

        // Assert
        await result.ShouldEqualInOrderAsync(Expected(30, 50));
        await Assert.That(reader.Reads.Count).IsEqualTo(0);
    }

    [Test]
    public async Task PrefetchAsync_PastEnd_ClampsToEnd()
    {
        // Act
        await stream.PrefetchAsync(90, 50);

        // Assert
        await reader.Reads.ShouldEqualInOrderAsync((90L, 10));
    }

    [Test]
    public async Task PrefetchAsync_InsidePrefetchedRange_DoesNotReadAgain()
    {
        // Arrange
        await stream.PrefetchAsync(10, 50);
        reader.Reads.Clear();

        // Act
        await stream.PrefetchAsync(20, 10);

        // Assert
        await Assert.That(reader.Reads.Count).IsEqualTo(0);
    }

    [Test]
    public async Task PrefetchAsync_NewRange_ReleasesPreviousRange()
    {
        // Arrange
        await stream.PrefetchAsync(0, 10);
        await stream.PrefetchAsync(50, 10);

        // Act
        var act = () => stream.Read(new byte[4], 0, 4);

        // Assert
        await Assert.That(act).Throws<NotSupportedException>();
    }

    [Test]
    public async Task PrefetchAsync_ZeroCount_ReleasesRange()
    {
        // Arrange
        await stream.PrefetchAsync(0, 10);

        // Act
        await stream.PrefetchAsync(0, 0);

        // Assert
        await Assert.That(() => stream.Read(new byte[4], 0, 4)).Throws<NotSupportedException>();
    }

    [Test]
    [Arguments(10L, SeekOrigin.Begin, 10L)]
    [Arguments(5L, SeekOrigin.Current, 25L)]
    [Arguments(-10L, SeekOrigin.End, 90L)]
    public async Task Seek_WithOrigin_MovesPosition(long offset, SeekOrigin origin, long expected)
    {
        // Arrange
        stream.Position = 20;

        // Act
        var result = stream.Seek(offset, origin);

        // Assert
        await Assert.That(result).IsEqualTo(expected);
        await Assert.That(stream.Position).IsEqualTo(expected);
    }

    [Test]
    public async Task Seek_BeforeStart_ThrowsIOException()
    {
        // Act
        var act = () => stream.Seek(-1, SeekOrigin.Begin);

        // Assert
        await Assert.That(act).Throws<IOException>();
    }

    [Test]
    public async Task Capabilities_WhenOpen_AreReadOnlySeekable()
    {
        // Assert
        await Assert.That(stream.CanRead).IsTrue();
        await Assert.That(stream.CanSeek).IsTrue();
        await Assert.That(stream.CanWrite).IsFalse();
        await Assert.That(stream.Length).IsEqualTo(FileSize);
    }

    [Test]
    public async Task Write_Always_ThrowsNotSupportedException()
    {
        // Act
        var act = () => stream.Write([1], 0, 1);

        // Assert
        await Assert.That(act).Throws<NotSupportedException>();
    }

    [Test]
    public async Task DisposeAsync_WhenOpen_ReleasesFile()
    {
        // Act
        await stream.DisposeAsync();

        // Assert
        await Assert.That(reader.Disposed).IsTrue();
        await Assert.That(stream.CanRead).IsFalse();
    }

    [Test]
    public async Task Dispose_WhenOpen_ReleasesFile()
    {
        // Act
        stream.Dispose();

        // Assert
        await Assert.That(reader.Disposed).IsTrue();
    }

    [Test]
    public async Task ReadAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        await stream.DisposeAsync();

        // Act
        var act = async () => await stream.ReadAsync(new byte[4]);

        // Assert
        await Assert.That(act).Throws<ObjectDisposedException>();
    }
}
