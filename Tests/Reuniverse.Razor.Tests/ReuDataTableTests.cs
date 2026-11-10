using AngleSharp.Dom;
using Microsoft.AspNetCore.Components;

namespace Reuniverse.Razor.Tests;

public class ReuDataTableTests : BunitContext
{
    public sealed record Run(string Player, int Rank, TimeSpan Time, string? Login = null);

    private static readonly Run[] Runs =
    [
        new("bigbang", 2, TimeSpan.FromSeconds(12.5), "bigbang1112"),
        new("Alpha", 1, TimeSpan.FromSeconds(11.2), "alpha_tm"),
        new("charlie", 3, TimeSpan.FromSeconds(13.9))
    ];

    public ReuDataTableTests()
    {
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    private IRenderedComponent<ReuDataTable<Run>> RenderTableWith(
        IEnumerable<Run>? items,
        Action<ComponentParameterCollectionBuilder<ReuDataTable<Run>>>? configure,
        params Action<ComponentParameterCollectionBuilder<ReuDataColumn<Run>>>[] columns)
    {
        return Render<ReuDataTable<Run>>(parameters =>
        {
            parameters.Add(x => x.Items, items);
            configure?.Invoke(parameters);

            foreach (var column in columns)
            {
                parameters.AddChildContent(column);
            }
        });
    }

    private IRenderedComponent<ReuDataTable<Run>> RenderTable(IEnumerable<Run>? items, params Action<ComponentParameterCollectionBuilder<ReuDataColumn<Run>>>[] columns)
    {
        return RenderTableWith(items, null, columns);
    }

    private static Action<ComponentParameterCollectionBuilder<ReuDataColumn<Run>>> PlayerColumn => column => column
        .Add(x => x.Title, "Player")
        .Add(x => x.Value, run => run.Player);

    private static Action<ComponentParameterCollectionBuilder<ReuDataColumn<Run>>> RankColumn => column => column
        .Add(x => x.Title, "Rank")
        .Add(x => x.Value, run => run.Rank);

    private static string[] Column(IRenderedComponent<ReuDataTable<Run>> cut, int index)
    {
        return cut.FindAll("tbody tr:not(.empty)").Select(row => row.QuerySelectorAll("td")[index].TextContent.Trim()).ToArray();
    }

    private static IElement Header(IRenderedComponent<ReuDataTable<Run>> cut, int index) => cut.FindAll("thead th")[index];

    private static IElement PageButton(IRenderedComponent<ReuDataTable<Run>> cut, string text)
    {
        return cut.FindAll(".pagination button").Single(button => button.TextContent.Trim() == text);
    }

    [Test]
    public async Task Render_WithItems_ShowsHeadersAndCells()
    {
        // Act
        var cut = RenderTable(Runs, PlayerColumn, RankColumn);

        // Assert
        await cut.FindAll("thead th").Select(x => x.TextContent.Trim().TrimEnd('↕').Trim()).ShouldEqualInOrderAsync("Player", "Rank");
        await Column(cut, 0).ShouldEqualInOrderAsync("bigbang", "Alpha", "charlie");
        await Column(cut, 1).ShouldEqualInOrderAsync("2", "1", "3");
    }

    [Test]
    public async Task Render_WithFormat_FormatsValue()
    {
        // Act
        var cut = RenderTable(Runs, column => column
            .Add(x => x.Title, "Time")
            .Add(x => x.Value, run => run.Time)
            .Add(x => x.Format, @"s\.ff"));

        // Assert
        await Column(cut, 0).ShouldEqualInOrderAsync("12.50", "11.20", "13.90");
    }

    [Test]
    public async Task Render_WithSecondary_ShowsItDimmedBelow()
    {
        // Act
        var cut = RenderTable(Runs, column => column
            .Add(x => x.Title, "Player")
            .Add(x => x.Value, run => run.Player)
            .Add(x => x.Secondary, run => run.Login));

        // Assert
        var cells = cut.FindAll("tbody td");
        await Assert.That(cells[0].QuerySelector("small")!.TextContent).IsEqualTo("bigbang1112");
        await Assert.That(cells[2].QuerySelector("small")).IsNull();
    }

    [Test]
    public async Task Render_WithTemplate_RendersIt()
    {
        // Act
        var cut = RenderTable(Runs, column => column
            .Add(x => x.Title, "Player")
            .Add(x => x.ChildContent, run => builder =>
            {
                builder.OpenElement(0, "a");
                builder.AddAttribute(1, "href", $"/users/{run.Player}");
                builder.AddContent(2, run.Player);
                builder.CloseElement();
            }));

        // Assert
        await Assert.That(cut.Find("tbody td a").GetAttribute("href")).IsEqualTo("/users/bigbang");
    }

    [Test]
    public async Task Render_WithHideTitle_KeepsTitleForScreenReaders()
    {
        // Act
        var cut = RenderTable(Runs, column => column
            .Add(x => x.Title, "Downloads")
            .Add(x => x.HideTitle, true));

        // Assert
        await Assert.That(Header(cut, 0).QuerySelector(".visually-hidden")!.TextContent).IsEqualTo("Downloads");
    }

    [Test]
    public async Task Render_WithoutItems_ShowsEmptyText()
    {
        // Act
        var cut = RenderTable([], PlayerColumn, RankColumn);

        // Assert
        var cell = cut.Find("tr.empty td");
        await Assert.That(cell.TextContent.Trim()).IsEqualTo("No items");
        await Assert.That(cell.GetAttribute("colspan")).IsEqualTo("2");
    }

    [Test]
    public async Task Render_WithEmptyContent_ShowsIt()
    {
        // Act
        var cut = RenderTableWith(null, parameters => parameters.Add(x => x.EmptyContent, "No records yet"), PlayerColumn);

        // Assert
        await Assert.That(cut.Find("tr.empty td").TextContent.Trim()).IsEqualTo("No records yet");
    }

    [Test]
    public async Task Render_WithRowClassFitAndAlignRight_AppliesThem()
    {
        // Act
        var cut = RenderTableWith(Runs,
            parameters => parameters.Add(x => x.RowClass, run => run.Login is null ? "removed" : null),
            column => column.Add(x => x.Title, "Rank").Add(x => x.Value, run => run.Rank).Add(x => x.Fit, true).Add(x => x.AlignRight, true));

        // Assert
        await Assert.That(cut.FindAll("tbody tr")[2].ClassList.Contains("removed")).IsTrue();
        await Assert.That(cut.FindAll("tbody tr")[0].ClassList.Contains("removed")).IsFalse();
        await Assert.That(Header(cut, 0).ClassList.Contains("fit")).IsTrue();
        await Assert.That(cut.Find("tbody td").ClassList.Contains("right")).IsTrue();
    }

    [Test]
    public async Task Render_WithMinWidth_SetsItOnTable()
    {
        // Act
        var cut = RenderTableWith(Runs, parameters => parameters.Add(x => x.MinWidth, "40rem"), PlayerColumn);

        // Assert
        await Assert.That(cut.Find("table").GetAttribute("style")).IsEqualTo("min-width: 40rem;");
    }

    [Test]
    public async Task ClickHeader_Once_SortsAscending()
    {
        // Arrange
        var cut = RenderTable(Runs, PlayerColumn, RankColumn);

        // Act
        Header(cut, 0).QuerySelector("button")!.Click();

        // Assert
        await Column(cut, 0).ShouldEqualInOrderAsync("Alpha", "bigbang", "charlie");
        await Assert.That(Header(cut, 0).GetAttribute("aria-sort")).IsEqualTo("ascending");
        await Assert.That(Header(cut, 1).HasAttribute("aria-sort")).IsFalse();
    }

    [Test]
    public async Task ClickHeader_Twice_SortsDescending()
    {
        // Arrange
        var cut = RenderTable(Runs, PlayerColumn, RankColumn);

        // Act
        Header(cut, 1).QuerySelector("button")!.Click();
        Header(cut, 1).QuerySelector("button")!.Click();

        // Assert
        await Column(cut, 1).ShouldEqualInOrderAsync("3", "2", "1");
        await Assert.That(Header(cut, 1).GetAttribute("aria-sort")).IsEqualTo("descending");
    }

    [Test]
    public async Task Render_WithDefaultSort_StartsSorted()
    {
        // Act
        var cut = RenderTable(Runs, PlayerColumn, column => column
            .Add(x => x.Title, "Rank")
            .Add(x => x.Value, run => run.Rank)
            .Add(x => x.DefaultSort, ReuSortDirection.Descending));

        // Assert
        await Column(cut, 1).ShouldEqualInOrderAsync("3", "2", "1");
    }

    [Test]
    public async Task ClickHeader_WithSortBy_SortsByItInsteadOfValue()
    {
        // Arrange
        var cut = RenderTable(Runs, column => column
            .Add(x => x.Title, "Player")
            .Add(x => x.Value, run => run.Player)
            .Add(x => x.SortBy, run => run.Rank));

        // Act
        Header(cut, 0).QuerySelector("button")!.Click();

        // Assert
        await Column(cut, 0).ShouldEqualInOrderAsync("Alpha", "bigbang", "charlie");
    }

    [Test]
    public async Task ClickHeader_WithNullValues_PutsThemFirst()
    {
        // Arrange
        var cut = RenderTable(Runs, column => column
            .Add(x => x.Title, "Login")
            .Add(x => x.Value, run => run.Login));

        // Act
        Header(cut, 0).QuerySelector("button")!.Click();

        // Assert
        await Column(cut, 0).ShouldEqualInOrderAsync("", "alpha_tm", "bigbang1112");
    }

    [Test]
    public async Task Render_ColumnWithoutValue_IsNotSortable()
    {
        // Act
        var cut = RenderTable(Runs, column => column
            .Add(x => x.Title, "Actions")
            .Add(x => x.ChildContent, run => builder => builder.AddContent(0, "…")));

        // Assert
        await Assert.That(Header(cut, 0).QuerySelector("button")).IsNull();
    }

    [Test]
    public async Task Render_WithSortableFalse_IsNotSortable()
    {
        // Act
        var cut = RenderTable(Runs, column => column
            .Add(x => x.Title, "Player")
            .Add(x => x.Value, run => run.Player)
            .Add(x => x.Sortable, false));

        // Assert
        await Assert.That(Header(cut, 0).QuerySelector("button")).IsNull();
    }

    [Test]
    public async Task Render_WithoutInteractivity_ShowsRowsWithoutButtons()
    {
        // Arrange
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        // Act
        var cut = RenderTableWith(Runs, parameters => parameters.Add(x => x.PageSize, 2), PlayerColumn);

        // Assert
        await Assert.That(cut.FindAll("button").Count).IsEqualTo(0);
        await Column(cut, 0).ShouldEqualInOrderAsync("bigbang", "Alpha");
    }

    [Test]
    public async Task Render_WithPageSize_ShowsFirstPageAndPages()
    {
        // Act
        var cut = RenderTableWith(Runs, parameters => parameters.Add(x => x.PageSize, 2), PlayerColumn);

        // Assert
        await Column(cut, 0).ShouldEqualInOrderAsync("bigbang", "Alpha");
        await cut.FindAll(".pagination button").Select(x => x.TextContent.Trim()).ShouldEqualInOrderAsync("Previous", "1", "2", "Next");
        await Assert.That(PageButton(cut, "1").GetAttribute("aria-current")).IsEqualTo("page");
        await Assert.That(PageButton(cut, "Previous").HasAttribute("disabled")).IsTrue();
        await Assert.That(cut.Find(".pagination small").TextContent).IsEqualTo("3 items");
    }

    [Test]
    public async Task ClickNext_OnFirstPage_ShowsSecondPage()
    {
        // Arrange
        var cut = RenderTableWith(Runs, parameters => parameters.Add(x => x.PageSize, 2), PlayerColumn);

        // Act
        PageButton(cut, "Next").Click();

        // Assert
        await Column(cut, 0).ShouldEqualInOrderAsync("charlie");
        await Assert.That(cut.Instance.CurrentPage).IsEqualTo(2);
        await Assert.That(PageButton(cut, "Next").HasAttribute("disabled")).IsTrue();
    }

    [Test]
    public async Task ClickHeader_OnLaterPage_GoesBackToFirstPage()
    {
        // Arrange
        var cut = RenderTableWith(Runs, parameters => parameters.Add(x => x.PageSize, 2), PlayerColumn);
        PageButton(cut, "2").Click();

        // Act
        Header(cut, 0).QuerySelector("button")!.Click();

        // Assert
        await Assert.That(cut.Instance.CurrentPage).IsEqualTo(1);
        await Column(cut, 0).ShouldEqualInOrderAsync("Alpha", "bigbang");
    }

    [Test]
    [Arguments(1, "Previous 1 2 … 10 Next")]
    [Arguments(3, "Previous 1 2 3 4 … 10 Next")]
    [Arguments(5, "Previous 1 … 4 5 6 … 10 Next")]
    [Arguments(10, "Previous 1 … 9 10 Next")]
    public async Task Render_ManyPages_ShowsGapsAroundCurrentPage(int current, string expected)
    {
        // Arrange
        var items = Enumerable.Range(1, 10).Select(x => new Run($"p{x}", x, TimeSpan.Zero)).ToArray();
        var cut = RenderTableWith(items, parameters => parameters.Add(x => x.PageSize, 1), PlayerColumn);

        // Act
        await cut.InvokeAsync(() => cut.Instance.GoToPageAsync(current));

        // Assert
        var labels = cut.FindAll(".pagination button, .pagination .gap").Select(x => x.TextContent.Trim());
        await Assert.That(string.Join(' ', labels)).IsEqualTo(expected);
    }

    [Test]
    public async Task Render_SinglePage_ShowsNoPagination()
    {
        // Act
        var cut = RenderTableWith(Runs, parameters => parameters.Add(x => x.PageSize, 10), PlayerColumn);

        // Assert
        await Assert.That(cut.FindAll(".pagination").Count).IsEqualTo(0);
    }

    [Test]
    public async Task Render_SinglePageWithShowCount_ShowsCountOnly()
    {
        // Act
        var cut = RenderTableWith(Runs, parameters => parameters.Add(x => x.ShowCount, true).Add(x => x.CountLabel, "records"), PlayerColumn);

        // Assert
        await Assert.That(cut.Find(".pagination small").TextContent).IsEqualTo("3 records");
        await Assert.That(cut.FindAll(".pagination button").Count).IsEqualTo(0);
    }

    [Test]
    public async Task Render_WithNewItems_ShowsThem()
    {
        // Arrange
        var cut = RenderTable(Runs, PlayerColumn);

        // Act
        cut.Render(parameters => parameters.Add(x => x.Items, [new Run("delta", 4, TimeSpan.Zero)]));

        // Assert
        await Column(cut, 0).ShouldEqualInOrderAsync("delta");
    }

    [Test]
    public async Task Render_WithItemsProvider_RequestsFirstPageWithDefaultSortOnce()
    {
        // Arrange
        var requests = new List<ReuDataTableRequest>();

        // Act
        var cut = RenderTableWith(null,
            parameters => parameters
                .Add(x => x.PageSize, 2)
                .Add(x => x.ItemsProvider, request =>
                {
                    requests.Add(request);
                    return ValueTask.FromResult(new ReuDataTableResult<Run>(Runs.Take(2).ToList(), 25));
                }),
            PlayerColumn,
            column => column.Add(x => x.Title, "Rank").Add(x => x.SortKey, "rank").Add(x => x.DefaultSort, ReuSortDirection.Descending));

        // Assert
        cut.WaitForState(() => cut.FindAll("tbody tr:not(.empty)").Count == 2);
        await Assert.That(requests.Count).IsEqualTo(1);
        await Assert.That(requests[0]).IsEqualTo(requests[0] with { Page = 1, PageSize = 2, SortBy = "rank", SortDirection = ReuSortDirection.Descending });
        await Assert.That(requests[0].Skip).IsEqualTo(0);
        await Assert.That(cut.Find(".pagination small").TextContent).IsEqualTo("25 items");
        await Assert.That(cut.Instance.PageCount).IsEqualTo(13);
    }

    [Test]
    public async Task ClickPage_WithItemsProvider_RequestsThatPage()
    {
        // Arrange
        var requests = new List<ReuDataTableRequest>();
        var cut = RenderTableWith(null,
            parameters => parameters
                .Add(x => x.PageSize, 10)
                .Add(x => x.ItemsProvider, request =>
                {
                    requests.Add(request);
                    return ValueTask.FromResult(new ReuDataTableResult<Run>(Runs, 30));
                }),
            PlayerColumn);
        cut.WaitForElement(".pagination");

        // Act
        PageButton(cut, "3").Click();

        // Assert
        await Assert.That(requests[^1].Page).IsEqualTo(3);
        await Assert.That(requests[^1].Skip).IsEqualTo(20);
    }

    [Test]
    public async Task ClickHeader_WithItemsProvider_RequestsSortByTitleAsKey()
    {
        // Arrange
        var requests = new List<ReuDataTableRequest>();
        var cut = RenderTableWith(null,
            parameters => parameters.Add(x => x.ItemsProvider, request =>
            {
                requests.Add(request);
                return ValueTask.FromResult(new ReuDataTableResult<Run>(Runs, 3));
            }),
            column => column.Add(x => x.Title, "Player"));
        cut.WaitForElement("thead button");

        // Act
        Header(cut, 0).QuerySelector("button")!.Click();

        // Assert
        await Assert.That(requests[^1].SortBy).IsEqualTo("Player");
        await Assert.That(requests[^1].SortDirection).IsEqualTo(ReuSortDirection.Ascending);
        await Assert.That(requests[^1].PageSize).IsNull();
    }

    [Test]
    public async Task Load_WhileEarlierLoadRuns_CancelsItAndIgnoresItsResult()
    {
        // Arrange
        var pending = new List<(ReuDataTableRequest Request, TaskCompletionSource<ReuDataTableResult<Run>> Result)>();
        var cut = RenderTableWith(null,
            parameters => parameters
                .Add(x => x.PageSize, 1)
                .Add(x => x.ItemsProvider, request =>
                {
                    var result = new TaskCompletionSource<ReuDataTableResult<Run>>();
                    pending.Add((request, result));
                    return new ValueTask<ReuDataTableResult<Run>>(result.Task);
                }),
            PlayerColumn);
        cut.WaitForState(() => pending.Count == 1);
        await cut.InvokeAsync(() => pending[0].Result.SetResult(new ReuDataTableResult<Run>([Runs[0]], 3)));

        // Act
        var later = cut.InvokeAsync(() => cut.Instance.GoToPageAsync(2));
        var latest = cut.InvokeAsync(() => cut.Instance.GoToPageAsync(3));
        await cut.InvokeAsync(() => pending[2].Result.SetResult(new ReuDataTableResult<Run>([Runs[2]], 3)));
        await cut.InvokeAsync(() => pending[1].Result.SetResult(new ReuDataTableResult<Run>([Runs[1]], 3)));
        await later;
        await latest;

        // Assert
        await Assert.That(pending[1].Request.CancellationToken.IsCancellationRequested).IsTrue();
        await Column(cut, 0).ShouldEqualInOrderAsync("charlie");
        await Assert.That(cut.FindAll(".loading").Count).IsEqualTo(0);
    }

    [Test]
    public async Task Render_ColumnOutsideTable_Throws()
    {
        // Act
        var act = () => Render<ReuDataColumn<Run>>();

        // Assert
        await Assert.That(act).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Render_WithAttributes_PassesThemToContainer()
    {
        // Act
        var cut = RenderTableWith(Runs, parameters => parameters.Add(x => x.CssClass, "records").AddUnmatched("id", "table"), PlayerColumn);

        // Assert
        var container = cut.Find(".reu-data-table");
        await Assert.That(container.ClassList.Contains("records")).IsTrue();
        await Assert.That(container.Id).IsEqualTo("table");
    }
}
