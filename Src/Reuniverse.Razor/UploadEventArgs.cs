namespace Reuniverse.Razor;

public sealed record UploadEventArgs(string FileName, byte[] Data, DateTimeOffset LastModified, string Type)
{
    /// <summary>
    /// The path of the file within the uploaded folder, including the folder name, such as <c>Maps/Campaign/A01.Map.Gbx</c>. Null when the file was not uploaded as part of a folder.
    /// </summary>
    public string? RelativePath { get; init; }
}