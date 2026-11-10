namespace Reuniverse.Razor;

/// <summary>
/// The page of items that a <see cref="ReuDataTable{TItem}"/> asks its items provider for.
/// </summary>
/// <param name="Page">The page number, starting at 1.</param>
/// <param name="PageSize">The number of items on a page, or null when the table is not paged and wants every item.</param>
/// <param name="SortBy">The <c>SortKey</c> (or <c>Title</c>) of the column to sort by, or null when unsorted.</param>
/// <param name="SortDirection">The direction to sort in.</param>
/// <param name="CancellationToken">Cancelled when a newer request replaces this one.</param>
public sealed record ReuDataTableRequest(int Page, int? PageSize, string? SortBy, ReuSortDirection SortDirection, CancellationToken CancellationToken)
{
    /// <summary>
    /// The number of items before the requested page, to skip.
    /// </summary>
    public int Skip => PageSize is null ? 0 : (Page - 1) * PageSize.Value;
}
