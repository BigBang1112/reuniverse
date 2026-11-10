namespace Reuniverse.Razor;

/// <summary>
/// A page of items for a <see cref="ReuDataTable{TItem}"/>, returned by its items provider.
/// </summary>
/// <param name="Items">The items on the requested page.</param>
/// <param name="TotalCount">The number of items across all pages.</param>
public sealed record ReuDataTableResult<TItem>(IReadOnlyList<TItem> Items, int TotalCount);
