namespace Reuniverse.Razor.Tests;

public class StreamEventArgsTests
{
    [Test]
    public async Task Dispose_WithStream_DisposesStream()
    {
        // Arrange
        var stream = new MemoryStream();
        var args = new StreamEventArgs("file.bin", 0, stream, DateTimeOffset.UnixEpoch);

        // Act
        args.Dispose();

        // Assert
        await Assert.That(stream.CanRead).IsFalse();
    }

    [Test]
    public async Task DisposeAsync_WithStream_DisposesStream()
    {
        // Arrange
        var stream = new MemoryStream();
        var args = new StreamEventArgs("file.bin", 0, stream, DateTimeOffset.UnixEpoch);

        // Act
        await args.DisposeAsync();

        // Assert
        await Assert.That(stream.CanRead).IsFalse();
    }
}
