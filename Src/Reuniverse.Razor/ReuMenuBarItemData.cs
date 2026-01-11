namespace Reuniverse.Razor;

public sealed class ReuMenuBarItemData
{
    public required string Name { get; init; }
    public string? Id { get; init; }
    public List<ReuMenuBarItemData> Items { get; init; } = [];
    public Func<Task>? Action { get; init; }
    public string? Href { get; init; }
}
