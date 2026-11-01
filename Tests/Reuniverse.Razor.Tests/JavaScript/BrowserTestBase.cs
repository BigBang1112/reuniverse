using Microsoft.Playwright;

namespace Reuniverse.Razor.Tests.JavaScript;

/// <summary>
/// Runs the real scripts in a real browser. Requests to a made-up origin are answered here, so the
/// pages get working storage and module imports without hosting anything.
/// </summary>
public abstract class BrowserTestBase
{
    protected const string TooltipSelector = ".reu-tooltip";

    private const string Origin = "http://reuniverse.test";

    private IBrowserContext context = default!;
    private string html = "";

    [ClassDataSource<BrowserFixture>(Shared = SharedType.PerTestSession)]
    public required BrowserFixture Fixture { get; init; }

    protected IPage Page { get; private set; } = default!;

    [Before(Test)]
    public async Task OpenPageAsync()
    {
        context = await Fixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1000, Height = 700 }
        });

        Page = await context.NewPageAsync();

        await Page.RouteAsync($"{Origin}/**", ServeAsync);
    }

    [After(Test)]
    public async Task ClosePageAsync()
    {
        await context.CloseAsync();
    }

    /// <summary>
    /// Opens the page with time standing still, so that timers only run when the test moves the clock.
    /// </summary>
    protected async Task GoToWithPausedClockAsync(string body, string head = "")
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        await Page.Clock.InstallAsync(new ClockInstallOptions { TimeDate = start });
        await GoToAsync(body, head);
        await Page.Clock.PauseAtAsync(start.AddMinutes(1));
    }

    protected async Task GoToAsync(string body, string head = "")
    {
        html = $"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8" />
                {head}
            </head>
            <body>{body}</body>
            </html>
            """;

        await Page.GotoAsync($"{Origin}/");
    }

    private async Task ServeAsync(IRoute route)
    {
        var path = new Uri(route.Request.Url).AbsolutePath;

        if (path == "/")
        {
            await route.FulfillAsync(new RouteFulfillOptions { ContentType = "text/html", Body = html });
            return;
        }

        var file = Path.Combine(AppContext.BaseDirectory, "Scripts", Path.GetFileName(path));

        if (!File.Exists(file))
        {
            await route.FulfillAsync(new RouteFulfillOptions { Status = 404 });
            return;
        }

        await route.FulfillAsync(new RouteFulfillOptions
        {
            ContentType = path.EndsWith(".css") ? "text/css" : "text/javascript",
            Body = await File.ReadAllTextAsync(file)
        });
    }

    protected async Task<double> GetStyleNumberAsync(string selector, string property)
    {
        return await Page.EvalOnSelectorAsync<double>(selector, $"element => parseFloat(element.style['{property}'])");
    }
}
