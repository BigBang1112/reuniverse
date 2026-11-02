using Microsoft.Playwright;

namespace Reuniverse.Razor.Tests.JavaScript;

public class ThemeScriptTests : BrowserTestBase
{
    private const string Script = """<script src="/reuniverse.theme.js"></script>""";

    private Task OpenAsync() => GoToAsync("", Script);

    private Task SaveThemeAsync(string theme) => Page.AddInitScriptAsync($"""localStorage.setItem("reu-theme", "{theme}")""");

    private Task PreferAsync(ColorScheme scheme) => Page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = scheme });

    private Task<string?> GetThemeAttributeAsync() => Page.EvaluateAsync<string?>("""document.documentElement.getAttribute("data-theme")""");

    [Test]
    [Arguments("light")]
    [Arguments("dark")]
    public async Task Load_WithSavedTheme_AppliesIt(string theme)
    {
        // Arrange
        await PreferAsync(theme == "light" ? ColorScheme.Dark : ColorScheme.Light);
        await SaveThemeAsync(theme);

        // Act
        await OpenAsync();

        // Assert
        await Assert.That(await GetThemeAttributeAsync()).IsEqualTo(theme);
    }

    [Test]
    public async Task Load_WithoutSavedThemeAndLightPreference_UsesLight()
    {
        // Arrange
        await PreferAsync(ColorScheme.Light);

        // Act
        await OpenAsync();

        // Assert
        await Assert.That(await GetThemeAttributeAsync()).IsEqualTo("light");
    }

    [Test]
    public async Task Load_WithoutSavedThemeAndDarkPreference_UsesDark()
    {
        // Arrange
        await PreferAsync(ColorScheme.Dark);

        // Act
        await OpenAsync();

        // Assert
        await Assert.That(await GetThemeAttributeAsync()).IsEqualTo("dark");
    }

    [Test]
    public async Task Load_WithBlockedStorage_UsesPreference()
    {
        // Arrange
        await PreferAsync(ColorScheme.Light);
        await Page.AddInitScriptAsync("""Object.defineProperty(window, "localStorage", { get() { throw new Error("blocked"); } })""");

        // Act
        await OpenAsync();

        // Assert
        await Assert.That(await GetThemeAttributeAsync()).IsEqualTo("light");
    }

    [Test]
    public async Task Get_AfterLoad_ReturnsAppliedTheme()
    {
        // Arrange
        await SaveThemeAsync("light");
        await OpenAsync();

        // Act
        var theme = await Page.EvaluateAsync<string>("reuTheme.get()");

        // Assert
        await Assert.That(theme).IsEqualTo("light");
    }

    [Test]
    public async Task Set_WithTheme_AppliesTheme()
    {
        // Arrange
        await PreferAsync(ColorScheme.Dark);
        await OpenAsync();

        // Act
        await Page.EvaluateAsync("""reuTheme.set("light")""");

        // Assert
        await Assert.That(await GetThemeAttributeAsync()).IsEqualTo("light");
    }

    [Test]
    public async Task Set_WithTheme_SavesTheme()
    {
        // Arrange
        await PreferAsync(ColorScheme.Dark);
        await OpenAsync();

        // Act
        await Page.EvaluateAsync("""reuTheme.set("light")""");

        // Assert
        await Assert.That(await Page.EvaluateAsync<string>("""localStorage.getItem("reu-theme")""")).IsEqualTo("light");
    }

    [Test]
    public async Task Toggle_FromDark_SwitchesToLight()
    {
        // Arrange
        await PreferAsync(ColorScheme.Dark);
        await OpenAsync();

        // Act
        var theme = await Page.EvaluateAsync<string>("reuTheme.toggle()");

        // Assert
        await Assert.That(theme).IsEqualTo("light");
    }

    [Test]
    public async Task Toggle_FromLight_SwitchesToDark()
    {
        // Arrange
        await PreferAsync(ColorScheme.Light);
        await OpenAsync();

        // Act
        var theme = await Page.EvaluateAsync<string>("reuTheme.toggle()");

        // Assert
        await Assert.That(theme).IsEqualTo("dark");
    }

    [Test]
    public async Task RemoveAttribute_LikeEnhancedNavigation_AppliesThemeAgain()
    {
        // Arrange
        await SaveThemeAsync("light");
        await OpenAsync();

        // Act
        await Page.EvaluateAsync("""document.documentElement.removeAttribute("data-theme")""");
        await Page.WaitForFunctionAsync("""document.documentElement.hasAttribute("data-theme")""");

        // Assert
        await Assert.That(await GetThemeAttributeAsync()).IsEqualTo("light");
    }
}
