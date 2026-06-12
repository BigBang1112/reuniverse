namespace Reuniverse.Razor;

/// <summary>
/// Represents an entry (file or folder) that can be displayed by a <see cref="ReuFileBrowser"/>.
/// </summary>
public interface IReuEntry
{
    /// <summary>
    /// The display name of the entry.
    /// </summary>
    string Name { get; }
}
