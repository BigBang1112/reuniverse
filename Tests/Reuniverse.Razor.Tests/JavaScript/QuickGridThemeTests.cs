using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;

namespace Reuniverse.Razor.Tests.JavaScript;

/// <summary>
/// Checks the Reu theme of QuickGrid in a real browser, together with the scoped styles of QuickGrid itself,
/// which load after reuniverse.css in an app, so that the theme has to be more specific to win over them.
/// </summary>
public class QuickGridThemeTests : BrowserTestBase
{
    private const string Head = """
        <link rel="stylesheet" href="/reuniverse.css" />
        <link rel="stylesheet" href="/QuickGrid.styles.css" />
        """;

    public sealed record Record(int Rank, string Player, string Login);

    private static readonly Record[] Records =
    [
        new(1, "BigBang1112", "bigbang1112"),
        new(2, "Hylis", "hylis"),
        new(3, "riolu", "riolu_tm")
    ];

    private static string RenderGrid(int itemsPerPage)
    {
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        var pagination = new PaginationState { ItemsPerPage = itemsPerPage };

        var grid = context.Render<QuickGrid<Record>>(p => p
            .Add(x => x.Items, Records.AsQueryable())
            .Add(x => x.Pagination, pagination)
            .Add(x => x.Theme, "reu")
            .AddChildContent<PropertyColumn<Record, int>>(c => c
                .Add(x => x.Title, "#")
                .Add(x => x.Property, r => r.Rank)
                .Add(x => x.Align, Align.End)
                .Add(x => x.Sortable, true)
                .Add(x => x.IsDefaultSortColumn, true))
            .AddChildContent<TemplateColumn<Record>>(c => c
                .Add(x => x.Title, "Player")
                .Add(x => x.ChildContent, r => $"{r.Player}<small>{r.Login}</small>")));

        var paginator = context.Render<Paginator>(p => p.Add(x => x.State, pagination));

        return grid.Markup + paginator.Markup;
    }

    private async Task OpenAsync(int itemsPerPage = 2)
    {
        await GoToAsync($"<main>{RenderGrid(itemsPerPage)}</main>", Head, """lang="en" data-theme="dark" """);
    }

    private Task<string> StyleAsync(string selector, string property, string? pseudoElement = null)
    {
        return Page.Locator(selector).First.EvaluateAsync<string>(
            "(element, [property, pseudoElement]) => getComputedStyle(element, pseudoElement).getPropertyValue(property)",
            new[] { property, pseudoElement });
    }

    // the color a CSS value, such as a variable of the theme, computes to
    private Task<string> ColorAsync(string value)
    {
        return Page.EvaluateAsync<string>("""
            value => {
                const probe = document.createElement("div");
                probe.style.backgroundColor = value;
                document.body.append(probe);
                const color = getComputedStyle(probe).backgroundColor;
                probe.remove();
                return color;
            }
            """, value);
    }

    [Test]
    public async Task Render_SortedColumn_ShowsIndicatorInTextColor()
    {
        // Act
        await OpenAsync();

        // Assert
        await Assert.That(await StyleAsync("th.col-sort-asc .sort-indicator", "background-image")).IsEqualTo("none");
        await Assert.That(await StyleAsync("th.col-sort-asc .sort-indicator", "background-color")).IsEqualTo(await StyleAsync("th.col-sort-asc", "color"));
        await Assert.That(await StyleAsync("th.col-sort-asc .sort-indicator", "mask-image")).Contains("svg");
        await Assert.That(await StyleAsync("th.col-sort-asc .sort-indicator", "opacity")).IsEqualTo("0.8");
    }

    [Test]
    public async Task Render_Paginator_LooksLikeButtons()
    {
        // Act
        await OpenAsync();

        // Assert
        await Assert.That(await StyleAsync(".paginator", "border-top-width")).IsEqualTo("0px");
        await Assert.That(await StyleAsync(".paginator .go-next", "background-image")).IsEqualTo("none");
        await Assert.That(await StyleAsync(".paginator .go-next", "background-color")).IsEqualTo(await ColorAsync("var(--color-button-bg)"));
        await Assert.That(await StyleAsync(".paginator .go-next", "width")).IsEqualTo(await StyleAsync(".paginator .go-next", "height"));
        await Assert.That(await StyleAsync(".paginator .go-next", "mask-image", "::before")).Contains("svg");
        await Assert.That(await StyleAsync(".paginator .go-next", "background-color", "::before")).IsEqualTo(await StyleAsync(".paginator .go-next", "color"));
    }

    [Test]
    public async Task Hover_PageButton_UsesButtonHoverColor()
    {
        // Arrange
        await OpenAsync();
        var expected = await ColorAsync("var(--color-button-bg-hover)");

        // Act
        await Page.Locator(".paginator .go-next").HoverAsync();

        // Assert
        await Page.WaitForFunctionAsync(
            "expected => getComputedStyle(document.querySelector('.paginator .go-next')).backgroundColor === expected",
            expected);
    }

    [Test]
    public async Task Render_LastPageWithFewerItems_HidesDividersOfEmptyRows()
    {
        // Act
        await OpenAsync(itemsPerPage: 5);

        // Assert
        await Assert.That(await Page.Locator("tbody > tr:not([aria-rowindex])").CountAsync()).IsEqualTo(2);
        await Assert.That(await StyleAsync("tbody > tr:not([aria-rowindex]) > td", "border-bottom-color")).IsEqualTo("rgba(0, 0, 0, 0)");
        await Assert.That(await StyleAsync("tbody > tr[aria-rowindex] > td", "border-bottom-color")).IsNotEqualTo("rgba(0, 0, 0, 0)");
    }

    [Test]
    public async Task Render_ColumnAlignedToEnd_KeepsItsAlignment()
    {
        // Act
        await OpenAsync();

        // Assert
        await Assert.That(await StyleAsync("tbody td.col-justify-end", "text-align")).IsEqualTo("right");
    }

    [Test]
    public async Task Render_SecondLine_IsDimmedBelow()
    {
        // Act
        await OpenAsync();

        // Assert
        await Assert.That(await StyleAsync("tbody td small", "display")).IsEqualTo("block");
        await Assert.That(await StyleAsync("tbody td small", "opacity")).IsEqualTo("0.7");
    }
}
