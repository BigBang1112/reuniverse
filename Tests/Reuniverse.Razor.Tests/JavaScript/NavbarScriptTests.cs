using Microsoft.Playwright;

namespace Reuniverse.Razor.Tests.JavaScript;

public class NavbarScriptTests : BrowserTestBase
{
    // the widths make the thresholds exact: 550px with labels, 220px with icons
    private const string Styles = """
        <style>
            body { margin: 0; }
            .reu-container { display: flex; box-sizing: border-box; }
            .nav-brand { flex: none; width: 100px; }
            nav { display: flex; flex: none; }
            nav a { display: block; flex: none; width: 150px; }
            .navbar[data-reu-nav-mode="icons"] nav a { width: 40px; }
            .navbar[data-reu-nav-mode="menu"] nav { position: absolute; top: 3rem; left: 0; visibility: hidden; }
            .navbar[data-reu-nav-open] nav { visibility: visible; }
        </style>
        <script src="/reuniverse.js" defer></script>
        """;

    private const string Navbar = """
        <div class="navbar">
            <div class="reu-container">
                <div class="nav-brand">Brand</div>
                <label class="nav-toggle"><input type="checkbox" aria-label="Menu" />Menu</label>
                <nav>
                    <a href="#one">One</a>
                    <a href="#two">Two</a>
                    <a href="#three">Three</a>
                </nav>
            </div>
        </div>
        <p id="away">Away</p>
        """;

    private const string SecondNavbar = """
        <div class="navbar" id="second">
            <div class="reu-container">
                <label class="nav-toggle"><input type="checkbox" aria-label="Menu" />Menu</label>
                <nav><a href="#one">One</a></nav>
            </div>
        </div>
        """;

    private async Task OpenAsync(int width)
    {
        await Page.SetViewportSizeAsync(width, 600);
        await GoToAsync(Navbar, Styles);
    }

    private Task<string?> GetModeAsync(string selector = ".navbar")
    {
        return Page.EvaluateAsync<string?>($"""document.querySelector("{selector}").getAttribute("data-reu-nav-mode")""");
    }

    private Task<bool> IsOpenAsync(string selector = ".navbar")
    {
        return Page.EvaluateAsync<bool>($"""document.querySelector("{selector}").hasAttribute("data-reu-nav-open")""");
    }

    private Task WaitForModeAsync(string mode)
    {
        return Page.WaitForFunctionAsync($"""document.querySelector(".navbar").getAttribute("data-reu-nav-mode") === "{mode}" """);
    }

    private Task ResizeAsync(int width) => Page.SetViewportSizeAsync(width, 600);

    private Task ClickBurgerAsync() => Page.Locator(".nav-toggle").First.ClickAsync();

    private async Task OpenBurgerAsync()
    {
        await OpenAsync(200);
        await ClickBurgerAsync();
    }

    private Task ResetAttributesAsync()
    {
        return Page.EvaluateAsync("""
            () => {
                const navbar = document.querySelector(".navbar");

                for (const name of navbar.getAttributeNames()) {
                    if (name.startsWith("data-reu-nav")) {
                        navbar.removeAttribute(name);
                    }
                }
            }
            """);
    }

    [Test]
    [Arguments(1000, "labels")]
    [Arguments(400, "icons")]
    [Arguments(200, "menu")]
    public async Task Load_WithAvailableWidth_PicksFittingMode(int width, string expectedMode)
    {
        // Act
        await OpenAsync(width);

        // Assert
        await Assert.That(await GetModeAsync()).IsEqualTo(expectedMode);
    }

    [Test]
    public async Task Load_WithBurger_SetsMenuWidth()
    {
        // Act
        await OpenAsync(200);

        // Assert
        await Assert.That(await Page.EvaluateAsync<string>("""document.querySelector(".navbar").style.getPropertyValue("--reu-nav-menu-width")""")).IsEqualTo("450px");
    }

    [Test]
    public async Task Load_WithoutContainer_LeavesElementAlone()
    {
        // Act
        await GoToAsync("""<div class="navbar"><nav></nav></div>""", Styles);

        // Assert
        await Assert.That(await GetModeAsync()).IsNull();
    }

    [Test]
    public async Task Update_AfterFinishing_LeavesNoSizingState()
    {
        // Act
        await OpenAsync(400);

        // Assert
        await Assert.That(await Page.EvaluateAsync<bool>("""document.querySelector(".navbar").hasAttribute("data-reu-nav-sizing")""")).IsFalse();
    }

    [Test]
    public async Task Resize_ToNarrowWindow_SwitchesToIcons()
    {
        // Arrange
        await OpenAsync(1000);

        // Act
        await ResizeAsync(400);
        await WaitForModeAsync("icons");

        // Assert
        await Assert.That(await GetModeAsync()).IsEqualTo("icons");
    }

    [Test]
    public async Task Resize_ToWideWindow_RestoresLabels()
    {
        // Arrange
        await OpenAsync(200);

        // Act
        await ResizeAsync(1000);
        await WaitForModeAsync("labels");

        // Assert
        await Assert.That(await GetModeAsync()).IsEqualTo("labels");
    }

    [Test]
    public async Task AddItem_WhenNoLongerFitting_SwitchesToIcons()
    {
        // Arrange
        await OpenAsync(600);

        // Act
        await Page.EvaluateAsync("""document.querySelector("nav").insertAdjacentHTML("beforeend", '<a href="#four">Four</a>')""");
        await WaitForModeAsync("icons");

        // Assert
        await Assert.That(await GetModeAsync()).IsEqualTo("icons");
    }

    [Test]
    public async Task FontLoaded_MakingLabelsWider_SwitchesToIcons()
    {
        // Arrange
        await OpenAsync(600);
        await Page.EvaluateAsync("""document.querySelectorAll("nav a").forEach(link => link.style.width = "200px")""");

        // Act
        await Page.EvaluateAsync("""document.fonts.dispatchEvent(new Event("loadingdone"))""");
        await Page.WaitForFunctionAsync("""document.querySelector(".navbar").getAttribute("data-reu-nav-mode") !== "labels" """);

        // Assert
        await Assert.That(await GetModeAsync()).IsNotEqualTo("labels");
    }

    [Test]
    public async Task ClickBurger_WhenClosed_OpensMenu()
    {
        // Act
        await OpenBurgerAsync();

        // Assert
        await Assert.That(await IsOpenAsync()).IsTrue();
    }

    [Test]
    public async Task ClickBurger_WhenOpen_ClosesMenu()
    {
        // Arrange
        await OpenBurgerAsync();

        // Act
        await ClickBurgerAsync();

        // Assert
        await Assert.That(await IsOpenAsync()).IsFalse();
    }

    [Test]
    public async Task PressSpace_OnFocusedBurger_OpensMenu()
    {
        // Arrange
        await OpenAsync(200);
        await Page.Locator(".nav-toggle input").FocusAsync();

        // Act
        await Page.Keyboard.PressAsync("Space");

        // Assert
        await Assert.That(await IsOpenAsync()).IsTrue();
    }

    [Test]
    public async Task ClickLink_InsideOpenMenu_ClosesMenu()
    {
        // Arrange
        await OpenBurgerAsync();

        // Act
        await Page.Locator("nav a").First.ClickAsync();

        // Assert
        await Assert.That(await IsOpenAsync()).IsFalse();
    }

    [Test]
    public async Task PressEscape_WithOpenMenu_ClosesMenu()
    {
        // Arrange
        await OpenBurgerAsync();

        // Act
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await Assert.That(await IsOpenAsync()).IsFalse();
    }

    [Test]
    public async Task PointerDown_OutsideOpenMenu_ClosesMenu()
    {
        // Arrange
        await OpenBurgerAsync();

        // Act
        await Page.Mouse.ClickAsync(100, 500);

        // Assert
        await Assert.That(await IsOpenAsync()).IsFalse();
    }

    [Test]
    public async Task PointerDown_InsideOpenMenu_KeepsMenuOpen()
    {
        // Arrange
        await OpenBurgerAsync();

        // Act
        await Page.Locator(".nav-brand").ClickAsync();

        // Assert
        await Assert.That(await IsOpenAsync()).IsTrue();
    }

    [Test]
    public async Task Resize_WithOpenMenuToWideWindow_ClosesMenu()
    {
        // Arrange
        await OpenBurgerAsync();

        // Act
        await ResizeAsync(1000);
        await WaitForModeAsync("labels");

        // Assert
        await Assert.That(await IsOpenAsync()).IsFalse();
    }

    [Test]
    public async Task ClickBurger_WithTwoNavbars_OpensOnlyThatOne()
    {
        // Arrange
        await Page.SetViewportSizeAsync(200, 600);
        await GoToAsync(Navbar + SecondNavbar, Styles);

        // Act
        await Page.Locator("#second .nav-toggle").ClickAsync();

        // Assert
        await Assert.That(await Page.Locator("#second input").IsCheckedAsync()).IsTrue();
        await Assert.That(await Page.Locator(".navbar:not(#second) input").IsCheckedAsync()).IsFalse();
    }

    [Test]
    public async Task AddNavbar_AfterLoad_EnhancesIt()
    {
        // Arrange
        await GoToAsync("", Styles);

        // Act
        await Page.EvaluateAsync($$"""document.body.insertAdjacentHTML("beforeend", `{{Navbar}}`)""");
        await Page.WaitForFunctionAsync("""document.querySelector(".navbar").hasAttribute("data-reu-nav-mode")""");

        // Assert
        await Assert.That(await GetModeAsync()).IsEqualTo("labels");
    }

    [Test]
    public async Task ResetAttributes_LikeEnhancedNavigation_RestoresMode()
    {
        // Arrange
        await OpenAsync(400);

        // Act
        await ResetAttributesAsync();
        await WaitForModeAsync("icons");

        // Assert
        await Assert.That(await GetModeAsync()).IsEqualTo("icons");
    }

    [Test]
    public async Task ResetAttributes_Repeatedly_DoesNotStackListeners()
    {
        // Arrange
        await Page.AddInitScriptAsync("""
            window.documentListeners = 0;
            const add = document.addEventListener.bind(document);
            document.addEventListener = (...args) => {
                window.documentListeners++;
                add(...args);
            };
            """);
        await OpenAsync(1000);
        var before = await Page.EvaluateAsync<int>("window.documentListeners");

        // Act
        for (var i = 0; i < 3; i++)
        {
            await ResetAttributesAsync();
            await WaitForModeAsync("labels");
        }

        // Assert
        await Assert.That(await Page.EvaluateAsync<int>("window.documentListeners")).IsEqualTo(before);
    }

    [Test]
    public async Task ClickBurger_AfterResetAttributes_StillWorks()
    {
        // Arrange
        await OpenAsync(200);
        await ResetAttributesAsync();
        await WaitForModeAsync("menu");

        // Act
        await ClickBurgerAsync();

        // Assert
        await Assert.That(await IsOpenAsync()).IsTrue();
    }
}
