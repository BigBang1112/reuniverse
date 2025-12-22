namespace Reuniverse.Razor;

public sealed record ReuObjectTreeItem(int Index, object? Value)
{
    public override string ToString() => $"#{Index} {Value}";
}
