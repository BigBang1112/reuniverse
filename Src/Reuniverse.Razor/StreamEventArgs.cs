namespace Reuniverse.Razor;

public sealed record StreamEventArgs(string FileName, long Size, Stream Stream, DateTimeOffset LastModified) : IDisposable, IAsyncDisposable
{
    public void Dispose()
    {
        Stream.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return Stream.DisposeAsync();
    }
}