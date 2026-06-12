namespace Reuniverse.Razor;

/// <summary>
/// Represents a folder entry in a <see cref="ReuFileBrowser"/> that can contain files and other folders.
/// </summary>
public interface IReuFolder : IReuEntry
{
    /// <summary>
    /// The entries contained within this folder. This is enumerated lazily whenever the folder is opened,
    /// so implementations may defer loading the contents until this is accessed.
    /// </summary>
    IEnumerable<IReuEntry> Entries { get; }
}
