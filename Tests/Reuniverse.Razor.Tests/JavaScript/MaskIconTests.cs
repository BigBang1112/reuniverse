using Microsoft.Playwright;

namespace Reuniverse.Razor.Tests.JavaScript;

public class MaskIconTests : BrowserTestBase
{
    private const string Icon = """
        <base href="/" />
        <link rel="stylesheet" href="/_content/Reuniverse.Razor/reuniverse.css" />
        """;

    [Test]
    public async Task Render_InlineMaskImage_ResolvesAgainstThePage()
    {
        // Arrange
        var requested = new List<string>();
        Page.Request += (_, request) => requested.Add(new Uri(request.Url).AbsolutePath);

        // Act
        await GoToAsync("""<span class="reu-mask-icon" style="width: 32px; height: 32px; mask-image: url('_content/Reuniverse.Razor/icons/menu.svg');"></span>""", Icon);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        await Assert.That(requested).Contains("/_content/Reuniverse.Razor/icons/menu.svg");
    }

    [Test]
    public async Task Render_WithoutOwnColor_UsesTextColor()
    {
        // Act
        await GoToAsync("""<span id="icon" class="reu-mask-icon" style="color: rgb(1, 2, 3);"></span>""", Icon);

        // Assert
        await Assert.That(await Page.Locator("#icon").EvaluateAsync<string>("element => getComputedStyle(element).backgroundColor")).IsEqualTo("rgb(1, 2, 3)");
    }

    [Test]
    public async Task Render_WithIconColor_PrefersIt()
    {
        // Act
        await GoToAsync("""<span id="icon" class="reu-mask-icon" style="color: rgb(1, 2, 3); --reu-icon-color: rgb(4, 5, 6);"></span>""", Icon);

        // Assert
        await Assert.That(await Page.Locator("#icon").EvaluateAsync<string>("element => getComputedStyle(element).backgroundColor")).IsEqualTo("rgb(4, 5, 6)");
    }
}
