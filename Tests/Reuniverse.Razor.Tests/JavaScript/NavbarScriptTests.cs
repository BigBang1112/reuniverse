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
            .navbar.reu-nav-icons nav a { width: 40px; }
            .navbar.reu-nav-menu nav { position: absolute; top: 3rem; left: 0; visibility: hidden; }
            .navbar.reu-nav-menu-open nav { visibility: visible; }
            #nav-toggle { display: none; }
        </style>
        <script src="/reuniverse.js" defer></script>
        """;

    private const string Navbar = """
        <div class="navbar">
            <div class="reu-container">
                <div class="nav-brand">Brand</div>
                <input id="nav-toggle" type="checkbox" />
                <label for="nav-toggle" class="nav-toggle">Menu</label>
                <nav>
                    <a href="#one">One</a>
                    <a href="#two">Two</a>
                    <a href="#three">Three</a>
                </nav>
            </div>
        </div>
        <p id="away">Away</p>
        """;

    private const string FakeBlazor = """
        <script>
            window.enhancedLoadHandlers = [];
            window.Blazor = { addEventListener: (name, handler) => window.enhancedLoadHandlers.push(handler) };
        </script>
        """;

    private async Task OpenAsync(int width, string head = "")
    {
        await Page.SetViewportSizeAsync(width, 600);
        await GoToAsync(Navbar, head + Styles);
    }

    private Task<bool> HasClassAsync(string name)
    {
        return Page.EvaluateAsync<bool>($"""document.querySelector(".navbar").classList.contains("{name}")""");
    }

    private Task WaitForClassAsync(string name, bool present = true)
    {
        return Page.WaitForFunctionAsync($"""document.querySelector(".navbar").classList.contains("{name}") === {(present ? "true" : "false")}""");
    }

    private async Task ResizeAsync(int width)
    {
        await Page.SetViewportSizeAsync(width, 600);
    }

    private async Task OpenBurgerAsync()
    {
        await OpenAsync(200);
        await Page.Locator("label[for='nav-toggle']").ClickAsync();
    }

    [Test]
    public async Task Load_WithRoomForLabels_ShowsLabels()
    {
        // Act
        await OpenAsync(1000);

        // Assert
        await Assert.That(await HasClassAsync("reu-nav-icons")).IsFalse();
        await Assert.That(await HasClassAsync("reu-nav-menu")).IsFalse();
    }

    [Test]
    public async Task Load_Always_SwitchesToResponsiveLayout()
    {
        // Act
        await OpenAsync(1000);

        // Assert
        await Assert.That(await HasClassAsync("reu-nav-responsive")).IsTrue();
    }

    [Test]
    public async Task Load_WithRoomForIconsOnly_ShowsIcons()
    {
        // Act
        await OpenAsync(400);

        // Assert
        await Assert.That(await HasClassAsync("reu-nav-icons")).IsTrue();
    }

    [Test]
    public async Task Load_WithoutRoomForIcons_ShowsBurger()
    {
        // Act
        await OpenAsync(200);

        // Assert
        await Assert.That(await HasClassAsync("reu-nav-menu")).IsTrue();
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
    public async Task Resize_ToNarrowWindow_SwitchesToIcons()
    {
        // Arrange
        await OpenAsync(1000);

        // Act
        await ResizeAsync(400);
        await WaitForClassAsync("reu-nav-icons");

        // Assert
        await Assert.That(await HasClassAsync("reu-nav-menu")).IsFalse();
    }

    [Test]
    public async Task Resize_ToWideWindow_RestoresLabels()
    {
        // Arrange
        await OpenAsync(200);

        // Act
        await ResizeAsync(1000);
        await WaitForClassAsync("reu-nav-menu", present: false);

        // Assert
        await Assert.That(await HasClassAsync("reu-nav-icons")).IsFalse();
    }

    [Test]
    public async Task AddItem_WhenNoLongerFitting_SwitchesToIcons()
    {
        // Arrange
        await OpenAsync(600);

        // Act
        await Page.EvaluateAsync("""document.querySelector("nav").insertAdjacentHTML("beforeend", '<a href="#four">Four</a>')""");
        await WaitForClassAsync("reu-nav-icons");

        // Assert
        await Assert.That(await HasClassAsync("reu-nav-menu")).IsFalse();
    }

    [Test]
    public async Task ClickBurger_WhenClosed_OpensMenu()
    {
        // Act
        await OpenBurgerAsync();

        // Assert
        await Assert.That(await HasClassAsync("reu-nav-menu-open")).IsTrue();
    }

    [Test]
    public async Task ClickBurger_WhenClosed_MarksLabelExpanded()
    {
        // Act
        await OpenBurgerAsync();

        // Assert
        await Assert.That(await Page.Locator("label[for='nav-toggle']").GetAttributeAsync("aria-expanded")).IsEqualTo("true");
    }

    [Test]
    public async Task ClickBurger_WhenOpen_ClosesMenu()
    {
        // Arrange
        await OpenBurgerAsync();

        // Act
        await Page.Locator("label[for='nav-toggle']").ClickAsync();

        // Assert
        await Assert.That(await HasClassAsync("reu-nav-menu-open")).IsFalse();
    }

    [Test]
    public async Task ClickLink_InsideOpenMenu_ClosesMenu()
    {
        // Arrange
        await OpenBurgerAsync();

        // Act
        await Page.Locator("nav a").First.ClickAsync();

        // Assert
        await Assert.That(await HasClassAsync("reu-nav-menu-open")).IsFalse();
    }

    [Test]
    public async Task PressEscape_WithOpenMenu_ClosesMenu()
    {
        // Arrange
        await OpenBurgerAsync();

        // Act
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await Assert.That(await HasClassAsync("reu-nav-menu-open")).IsFalse();
    }

    [Test]
    public async Task PointerDown_OutsideOpenMenu_ClosesMenu()
    {
        // Arrange
        await OpenBurgerAsync();

        // Act
        await Page.Mouse.ClickAsync(100, 500);

        // Assert
        await Assert.That(await HasClassAsync("reu-nav-menu-open")).IsFalse();
    }

    [Test]
    public async Task PointerDown_InsideOpenMenu_KeepsMenuOpen()
    {
        // Arrange
        await OpenBurgerAsync();

        // Act
        await Page.Locator(".nav-brand").ClickAsync();

        // Assert
        await Assert.That(await HasClassAsync("reu-nav-menu-open")).IsTrue();
    }

    [Test]
    public async Task Resize_WithOpenMenuToWideWindow_ClosesMenu()
    {
        // Arrange
        await OpenBurgerAsync();

        // Act
        await ResizeAsync(1000);
        await WaitForClassAsync("reu-nav-menu", present: false);

        // Assert
        await Assert.That(await Page.Locator("#nav-toggle").IsCheckedAsync()).IsFalse();
    }

    [Test]
    public async Task AddNavbar_AfterLoad_EnhancesIt()
    {
        // Arrange
        await GoToAsync("", Styles);

        // Act
        await Page.EvaluateAsync($$"""document.body.insertAdjacentHTML("beforeend", `{{Navbar}}`)""");
        await WaitForClassAsync("reu-nav-responsive");

        // Assert
        await Assert.That(await HasClassAsync("reu-nav-responsive")).IsTrue();
    }

    [Test]
    public async Task EnhancedLoad_AfterClassWasReset_EnhancesNavbarAgain()
    {
        // Arrange
        await OpenAsync(1000, FakeBlazor);
        await Page.EvaluateAsync("""document.querySelector(".navbar").className = "navbar" """);

        // Act
        await Page.EvaluateAsync("window.enhancedLoadHandlers.forEach(handler => handler())");

        // Assert
        await Assert.That(await HasClassAsync("reu-nav-responsive")).IsTrue();
    }

    [Test]
    [Skip("Known bug: the only guard is a class that enhanced navigation resets, so every navigation adds another set of listeners and observers")]
    public async Task EnhancedLoad_Repeatedly_DoesNotStackListeners()
    {
        // Arrange
        await Page.AddInitScriptAsync("""
            window.keydownListeners = 0;
            const add = document.addEventListener.bind(document);
            document.addEventListener = (name, ...rest) => {
                if (name === "keydown") {
                    window.keydownListeners++;
                }

                add(name, ...rest);
            };
            """);
        await OpenAsync(1000, FakeBlazor);
        var before = await Page.EvaluateAsync<int>("window.keydownListeners");

        // Act
        await Page.EvaluateAsync("""
            for (let i = 0; i < 3; i++) {
                document.querySelector(".navbar").className = "navbar";
                window.enhancedLoadHandlers.forEach(handler => handler());
            }
            """);

        // Assert
        await Assert.That(await Page.EvaluateAsync<int>("window.keydownListeners")).IsEqualTo(before);
    }
}
