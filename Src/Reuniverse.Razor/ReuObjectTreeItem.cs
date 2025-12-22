namespace Reuniverse.Razor;

public sealed record ReuObjectTreeItem(object Parent, int Index, object? Value)
{
    public override string ToString() => $"#{Index} {Value}";
}
