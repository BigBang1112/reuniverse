namespace Reuniverse.Razor;

/// <summary>
/// The selected bytes of a <see cref="ReuHexEditor"/>. At least one byte is always selected while there is data.
/// </summary>
/// <param name="Start">The offset of the first selected byte.</param>
/// <param name="Length">The number of selected bytes.</param>
public readonly record struct ReuHexSelection(int Start, int Length)
{
    /// <summary>
    /// The offset right after the last selected byte.
    /// </summary>
    public int End => Start + Length;
}
