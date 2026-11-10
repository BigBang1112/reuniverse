namespace Reuniverse.Razor;

/// <summary>
/// A range of bytes that <see cref="ReuHexEditor"/> shows with a background color, such as a chunk or a known field.
/// </summary>
/// <param name="Offset">The offset of the first byte.</param>
/// <param name="Length">The number of bytes.</param>
/// <param name="Color">The background color as any CSS color. Defaults to a translucent yellow that suits both themes.</param>
/// <param name="Tooltip">The text shown while one of the bytes is hovered, such as the name of the field.</param>
public sealed record ReuHexHighlight(int Offset, int Length, string? Color = null, string? Tooltip = null)
{
    internal bool Contains(int offset) => offset >= Offset && offset - Offset < Length;
}
