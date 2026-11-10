using Microsoft.JSInterop;

namespace Reuniverse.Razor.Tests;

public class JSFileReaderTests
{
    private readonly FakeJSObjectReference file = new();

    [Test]
    public async Task ReadAsync_WithBytes_CopiesReturnedBytes()
    {
        // Arrange
        object?[]? received = null;
        file.Handler = (_, args) => { received = args; return new byte[] { 1, 2, 3 }; };
        var reader = new JSFileReader(file, useStreamReference: false);
        var buffer = new byte[3];

        // Act
        var read = await reader.ReadAsync(10, buffer, CancellationToken.None);

        // Assert
        await Assert.That(read).IsEqualTo(3);
        await buffer.ShouldEqualInOrderAsync<byte>(1, 2, 3);
        await file.Calls.ShouldEqualInOrderAsync("read");
        await received!.ShouldEqualInOrderAsync<object?>(10L, 3);
    }

    [Test]
    public async Task ReadAsync_WithBytesLongerThanAsked_IgnoresRest()
    {
        // Arrange
        file.Handler = (_, _) => new byte[] { 1, 2, 3, 4, 5 };
        var reader = new JSFileReader(file, useStreamReference: false);
        var buffer = new byte[2];

        // Act
        var read = await reader.ReadAsync(0, buffer, CancellationToken.None);

        // Assert
        await Assert.That(read).IsEqualTo(2);
        await buffer.ShouldEqualInOrderAsync<byte>(1, 2);
    }

    [Test]
    public async Task ReadAsync_WithStreamReference_ReadsSlice()
    {
        // Arrange
        var slice = new FakeJSStreamReference([4, 5, 6]);
        file.Handler = (_, _) => slice;
        var reader = new JSFileReader(file, useStreamReference: true);
        var buffer = new byte[3];

        // Act
        var read = await reader.ReadAsync(0, buffer, CancellationToken.None);

        // Assert
        await Assert.That(read).IsEqualTo(3);
        await buffer.ShouldEqualInOrderAsync<byte>(4, 5, 6);
        await file.Calls.ShouldEqualInOrderAsync("slice");
        await Assert.That(slice.MaxAllowedSize).IsEqualTo(3);
    }

    [Test]
    public async Task ReadAsync_WithStreamLongerThanAsked_Throws()
    {
        // Arrange
        file.Handler = (_, _) => new FakeJSStreamReference([1, 2, 3, 4, 5]);
        var reader = new JSFileReader(file, useStreamReference: true);

        // Act
        var act = async () => await reader.ReadAsync(0, new byte[2], CancellationToken.None);

        // Assert
        await Assert.That(act).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task ReadAsync_WhenScriptFails_ThrowsIOException()
    {
        // Arrange
        file.Handler = (_, _) => throw new JSException("NotReadableError");
        var reader = new JSFileReader(file, useStreamReference: false);

        // Act
        var act = async () => await reader.ReadAsync(0, new byte[2], CancellationToken.None);

        // Assert
        await Assert.That(act).Throws<IOException>();
    }

    [Test]
    public async Task DisposeAsync_Always_ReleasesFileReference()
    {
        // Arrange
        var reader = new JSFileReader(file, useStreamReference: false);

        // Act
        await reader.DisposeAsync();

        // Assert
        await Assert.That(file.Disposed).IsTrue();
    }
}
