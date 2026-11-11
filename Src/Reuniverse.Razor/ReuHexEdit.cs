namespace Reuniverse.Razor;

/// <summary>
/// A byte changed through a <see cref="ReuHexEditor"/>.
/// </summary>
/// <param name="Offset">The offset of the byte.</param>
/// <param name="OldValue">The value before the change.</param>
/// <param name="NewValue">The value after the change.</param>
public sealed record ReuHexEdit(int Offset, byte OldValue, byte NewValue);
