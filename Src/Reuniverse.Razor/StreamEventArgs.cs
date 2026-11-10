namespace Reuniverse.Razor;

/// <summary>
/// A file opened as a stream. Whoever handles the event owns the stream, and disposing the arguments disposes it.
/// </summary>
/// <param name="FileName">The name of the file.</param>
/// <param name="Size">The size of the file in bytes, as reported by the client.</param>
/// <param name="Stream">The file contents. From the upload components, this is a seekable <see cref="ReuBrowserFileStream"/>.</param>
/// <param name="LastModified">When the file was last modified.</param>
public sealed record StreamEventArgs(string FileName, long Size, Stream Stream, DateTimeOffset LastModified) : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// The path of the file within the uploaded folder, including the folder name, such as <c>Maps/Campaign/A01.Map.Gbx</c>. Null when the file was not uploaded as part of a folder.
    /// </summary>
    public string? RelativePath { get; init; }

    public void Dispose()
    {
        Stream.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return Stream.DisposeAsync();
    }
}