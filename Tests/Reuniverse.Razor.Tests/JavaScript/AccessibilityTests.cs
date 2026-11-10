using System.Text;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;

namespace Reuniverse.Razor.Tests.JavaScript;

/// <summary>
/// Runs axe on the markup the components render, with the real stylesheets in both themes,
/// against the WCAG 2.x A and AA rules. A failure lists every violation with the elements it was found on.
/// Color contrast is not checked for now, as these colors are below the 4.5:1 that AA asks for normal text:
/// <list type="bullet">
/// <item>the blue button in the light theme (white on #3a83d1, 3.9:1)</item>
/// <item>the yellow button in the dark theme (white on yellow, 3.1:1)</item>
/// <item>the timestamps and the TRC and DBG badges of ReuLogs, in the muted text color (2.3 to 2.8:1 in both themes)</item>
/// <item>the ERR badge of ReuLogs in the dark theme (white on #ff4c4c, 3.3:1)</item>
/// <item>the type names of ReuFriendlyType and ReuObjectTree in the light theme (#267f99, 4.0 to 4.2:1)</item>
/// </list>
/// Remove the color-contrast rule from <see cref="Options"/> to check them again.
/// </summary>
public class AccessibilityTests : BrowserTestBase
{
    private const string Head = """
        <title>Accessibility</title>
        <link rel="stylesheet" href="/reuniverse.css" />
        <link rel="stylesheet" href="/Reuniverse.Razor.styles.css" />
        <script src="/reuniverse.js" defer></script>
        """;

    private static readonly AxeRunOptions Options = new()
    {
        RunOnly = new RunOnlyOptions
        {
            Type = "tag",
            Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa"]
        },
        // the known contrast issues are listed above
        Rules = new Dictionary<string, RuleOptions>
        {
            ["color-contrast"] = new RuleOptions { Enabled = false }
        }
    };

    private sealed record TestFile(string Name, long? Size = null, DateTimeOffset? LastModified = null) : IReuFile;

    private sealed class TestFolder(string name, params IReuEntry[] entries) : IReuFolder
    {
        public string Name { get; } = name;
        public IEnumerable<IReuEntry> Entries { get; } = entries;
    }

    public sealed record Record(int Rank, string Player, string Login, TimeSpan Time);

    private sealed class Model
    {
        public string Name { get; set; } = "Example";
        public int Count { get; set; } = 3;
        public bool Enabled { get; set; } = true;
        public List<int> Values { get; set; } = [1, 2];
        public Model? Child { get; set; }
    }

    /// <summary>
    /// Renders with bUnit, interactively unless told otherwise, and returns the markup.
    /// </summary>
    private static string RenderMarkup<TComponent>(Action<ComponentParameterCollectionBuilder<TComponent>>? parameters = null, bool interactive = true)
        where TComponent : IComponent
    {
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.SetRendererInfo(new RendererInfo(interactive ? "Server" : "Static", interactive));
        return context.Render(parameters ?? (_ => { })).Markup;
    }

    private async Task AssertNoViolationsAsync(string markup, string theme)
    {
        await GoToAsync($"<main>{markup}</main>", Head, $"""lang="en" data-theme="{theme}" """);

        var result = await Page.RunAxe(Options);

        if (result.Violations.Length == 0)
        {
            return;
        }

        var report = new StringBuilder();

        foreach (var violation in result.Violations)
        {
            report.AppendLine($"{violation.Id} ({violation.Impact}): {violation.Help}");

            foreach (var node in violation.Nodes)
            {
                var checks = (node.Any ?? []).Concat(node.All ?? []).Concat(node.None ?? []).Select(check => check.Message);
                report.AppendLine($"    {node.Target}: {string.Join("; ", checks)}");
            }
        }

        Assert.Fail(report.ToString());
    }

    [Test]
    [Arguments("dark")]
    [Arguments("light")]
    public async Task Buttons_HaveNoViolations(string theme)
    {
        var markup = string.Concat(
            RenderMarkup<ReuButton>(p => p.AddChildContent("Default")),
            RenderMarkup<ReuButton>(p => p.Add(x => x.Variant, ReuButtonVariant.Blue).AddChildContent("Blue")),
            RenderMarkup<ReuButton>(p => p.Add(x => x.Variant, ReuButtonVariant.Yellow).AddChildContent("Yellow")),
            RenderMarkup<ReuButton>(p => p.Add(x => x.Variant, ReuButtonVariant.Red).AddChildContent("Red")),
            RenderMarkup<ReuButton>(p => p.Add(x => x.Enable, false).AddChildContent("Disabled")),
            RenderMarkup<ReuButton>(p => p.Add(x => x.Href, "/page").AddChildContent("Link")),
            RenderMarkup<ReuThemeToggle>());

        await AssertNoViolationsAsync(markup, theme);
    }

    [Test]
    [Arguments("dark")]
    [Arguments("light")]
    public async Task FormControls_HaveNoViolations(string theme)
    {
        var markup = string.Concat(
            RenderMarkup<ReuCheckbox>(p => p.Add(x => x.IsChecked, true).AddChildContent("Remember me")),
            RenderMarkup<ReuRadio>(p => p.Add(x => x.Name, "car").Add(x => x.IsChecked, true).AddChildContent("Stadium")),
            RenderMarkup<ReuRadio>(p => p.Add(x => x.Name, "car").AddChildContent("Snow")));

        await AssertNoViolationsAsync(markup, theme);
    }

    [Test]
    [Arguments("dark")]
    [Arguments("light")]
    public async Task Layout_HasNoViolations(string theme)
    {
        var markup = string.Concat(
            RenderMarkup<ReuNavMenu>(p => p
                .Add(x => x.Brand, "reuniverse")
                .AddChildContent<ReuNavItem>(i => i.Add(x => x.Name, "Home").Add(x => x.Href, "").Add(x => x.IconType, IconType.Home).Add(x => x.NavLinkMatch, NavLinkMatch.All))
                .AddChildContent<ReuNavItem>(i => i.Add(x => x.Name, "Tools").Add(x => x.Href, "tools").Add(x => x.IconType, IconType.Folder)
                    .AddChildContent<ReuNavItem>(s => s.Add(x => x.Name, "Viewer").Add(x => x.Href, "tools/viewer")))
                .AddChildContent<ReuNavItem>(i => i.Add(x => x.Name, "GitHub").Add(x => x.Href, "https://github.com").Add(x => x.IconType, IconType.GitHub).Add(x => x.IsNavLink, false).Add(x => x.IconOnly, true))),
            RenderMarkup<ReuList>(p => p
                .Add(x => x.Panel, true)
                .AddChildContent<ReuListItem>(i => i.Add(x => x.Active, true).AddChildContent("Active"))
                .AddChildContent<ReuListItem>(i => i.AddChildContent("Other"))
                .AddChildContent<ReuListItem>(i => i.Add(x => x.Enable, false).AddChildContent("Disabled"))),
            RenderMarkup<ReuFooter>(p => p
                .Add(x => x.CopyrightContent, "© 2026 Reuniverse")
                .Add(x => x.LinkContent, builder =>
                {
                    builder.OpenElement(0, "a");
                    builder.AddAttribute(1, "href", "/terms");
                    builder.AddContent(2, "Terms of Service");
                    builder.CloseElement();
                })));

        await AssertNoViolationsAsync(markup, theme);
    }

    [Test]
    [Arguments("dark")]
    [Arguments("light")]
    public async Task Disclosures_HaveNoViolations(string theme)
    {
        var markup = string.Concat(
            RenderMarkup<ReuTabs>(p => p
                .AddChildContent<ReuTab>(t => t.Add(x => x.TitleContent, "Overview").Add(x => x.ChildContent, "Overview content"))
                .AddChildContent<ReuTab>(t => t.Add(x => x.TitleContent, "Details").Add(x => x.ShowCloseButton, true).Add(x => x.ChildContent, "Details content"))),
            RenderMarkup<ReuExpansionPanels>(p => p
                .AddChildContent<ReuExpansionPanel>(e => e.Add(x => x.TitleContent, "First").Add(x => x.Expanded, true).Add(x => x.ChildContent, "First content"))
                .AddChildContent<ReuExpansionPanel>(e => e.Add(x => x.TitleContent, "Second").Add(x => x.ChildContent, "Second content"))),
            RenderMarkup<ReuMenuBar>(p => p.Add(x => x.Items,
            [
                new ReuMenuBarItemData { Name = "File", Items = [new() { Name = "Open" }, new() { Name = "Save" }] },
                new ReuMenuBarItemData { Name = "Help", Href = "/help" }
            ])),
            RenderMarkup<ReuTooltip>(p => p.Add(x => x.Text, "Saved 2 minutes ago").AddChildContent("Saved")));

        await AssertNoViolationsAsync(markup, theme);
    }

    [Test]
    [Arguments("dark")]
    [Arguments("light")]
    public async Task Modal_HasNoViolations(string theme)
    {
        var markup = RenderMarkup<ReuModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.Title, "Discard changes?")
            .Add(x => x.ChildContent, "Unsaved changes will be lost.")
            .Add(x => x.FooterContent, "Footer"));

        await AssertNoViolationsAsync(markup, theme);
    }

    [Test]
    [Arguments("dark")]
    [Arguments("light")]
    public async Task Uploads_HaveNoViolations(string theme)
    {
        var markup = string.Concat(
            RenderMarkup<ReuUploadButton>(p => p.AddChildContent("Open files...")),
            RenderMarkup<ReuUploadArea>(p => p.AddChildContent("Drop files here or click to browse")),
            RenderMarkup<ReuLoader>());

        await AssertNoViolationsAsync(markup, theme);
    }

    [Test]
    [Arguments("dark")]
    [Arguments("light")]
    public async Task DataViews_HaveNoViolations(string theme)
    {
        Record[] records =
        [
            new(1, "BigBang1112", "bigbang1112", TimeSpan.FromSeconds(43.12)),
            new(2, "Hylis", "hylis", TimeSpan.FromSeconds(43.48)),
            new(3, "riolu", "riolu_tm", TimeSpan.FromSeconds(43.91))
        ];

        var markup = string.Concat(
            RenderMarkup<ReuDataTable<Record>>(p => p
                .Add(x => x.Items, records)
                .Add(x => x.PageSize, 2)
                .AddChildContent<ReuDataColumn<Record>>(c => c.Add(x => x.Title, "#").Add(x => x.Value, r => r.Rank).Add(x => x.DefaultSort, ReuSortDirection.Ascending))
                .AddChildContent<ReuDataColumn<Record>>(c => c.Add(x => x.Title, "Player").Add(x => x.Value, r => r.Player).Add(x => x.Secondary, r => r.Login))
                .AddChildContent<ReuDataColumn<Record>>(c => c.Add(x => x.Title, "Time").Add(x => x.Value, r => r.Time).Add(x => x.Format, @"m\:ss\.fff"))),
            RenderMarkup<ReuFileBrowser>(p => p
                .Add(x => x.Root, new TestFolder("", new TestFolder("Maps", new TestFile("A01.Map.Gbx", 1024)), new TestFile("readme.md", 12)))
                .AddUnmatched("style", "height: 200px")),
            RenderMarkup<ReuFriendlyType>(p => p.Add(x => x.Type, typeof(Dictionary<string, List<int>>))));

        await AssertNoViolationsAsync(markup, theme);
    }

    [Test]
    [Arguments("dark")]
    [Arguments("light")]
    public async Task Logs_HaveNoViolations(string theme)
    {
        static ReuLogEntry Entry(string message, LogLevel level) => new() { Message = message, Level = level, Category = "App" };

        var markup = RenderMarkup<ReuLogs>(p => p
            .Add(x => x.Entries,
            [
                Entry("Entering render loop", LogLevel.Trace),
                Entry("Cache miss", LogLevel.Debug),
                Entry("Application started", LogLevel.Information),
                Entry("Disk space is low", LogLevel.Warning),
                Entry("Failed to connect", LogLevel.Error),
                Entry("Shutting down", LogLevel.Critical)
            ])
            .AddUnmatched("style", "height: 220px"));

        await AssertNoViolationsAsync(markup, theme);
    }

    [Test]
    [Arguments("dark")]
    [Arguments("light")]
    public async Task ObjectTree_HasNoViolations(string theme)
    {
        var markup = RenderMarkup<ReuObjectTree>(p => p
            .Add(x => x.Value, new Model { Child = new Model() })
            .Add(x => x.IsExpandedByDefault, true));

        await AssertNoViolationsAsync(markup, theme);
    }

    [Test]
    [Arguments("dark")]
    [Arguments("light")]
    public async Task Editors_HaveNoViolations(string theme)
    {
        var data = Enumerable.Range(0, 64).Select(x => (byte)x).ToArray();

        var markup = string.Concat(
            RenderMarkup<ReuSplitPane>(p => p
                .Add(x => x.FirstContent, "Files")
                .Add(x => x.SecondContent, "Details")
                .AddUnmatched("style", "height: 120px")),
            RenderMarkup<ReuHexEditor>(p => p
                .Add(x => x.Data, data)
                .Add(x => x.Highlights, [new ReuHexHighlight(0, 4, Tooltip: "Magic")])
                .AddUnmatched("style", "height: 200px")));

        await AssertNoViolationsAsync(markup, theme);
    }
}
