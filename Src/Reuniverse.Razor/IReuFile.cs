namespace Reuniverse.Razor;

/// <summary>
/// Represents a file entry in a <see cref="ReuFileBrowser"/>.
/// </summary>
public interface IReuFile : IReuEntry
{
    /// <summary>
    /// The size of the file in bytes, or <see langword="null"/> if unknown.
    /// </summary>
    long? Size { get; }

    /// <summary>
    /// The last modification time of the file, or <see langword="null"/> if unknown.
    /// </summary>
    DateTimeOffset? LastModified { get; }
}
