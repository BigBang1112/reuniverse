using Microsoft.Playwright;

namespace Reuniverse.Razor.Tests.JavaScript;

public class NavbarSubmenuScriptTests : BrowserTestBase
{
    // the rules that show a submenu, as ReuNavItem and ReuNavMenu ship them
    private const string Head = """
        <style>
            body { margin: 0; }
            .reu-container { display: flex; box-sizing: border-box; }
            .nav-brand { flex: none; width: 100px; }
            nav { display: flex; flex: none; }
            .nav-item { flex: none; width: 150px; }
            .navbar[data-reu-nav-mode="icons"] .nav-item { width: 40px; }
            .navbar[data-reu-nav-mode="menu"] nav { flex-direction: column; position: absolute; top: 3rem; left: 0; visibility: hidden; transform: scale(0.97); --reu-nav-submenu-visibility: hidden; --reu-nav-submenu-transition: none; }
            .navbar[data-reu-nav-mode="menu"] .nav-item { width: auto; }
            .navbar[data-reu-nav-open] nav { visibility: visible; transform: none; --reu-nav-submenu-visibility: visible; }
            .nav-submenu { position: relative; }
            .nav-submenu-items { position: absolute; top: 2rem; visibility: var(--reu-nav-submenu-visibility, hidden); transition: var(--reu-nav-submenu-transition, visibility 0s linear 0.1s); }
            .nav-submenu:hover > .nav-submenu-items,
            .nav-submenu:has(:focus-visible) > .nav-submenu-items,
            .nav-submenu[data-reu-nav-submenu="open"] > .nav-submenu-items { visibility: visible; transition-delay: 0s; }
            .nav-submenu[data-reu-nav-submenu="closed"] > .nav-submenu-items { visibility: var(--reu-nav-submenu-visibility, hidden); transition-delay: 0s; }
            .navbar[data-reu-nav-mode="menu"] .nav-submenu-items { position: static; }
        </style>
        <script src="/reuniverse.js" defer></script>
        """;

    private const string Navbar = """
        <div class="navbar">
            <div class="reu-container">
                <div class="nav-brand">Brand</div>
                <label class="nav-toggle"><input type="checkbox" aria-label="Menu" />Menu</label>
                <nav>
                    <div class="nav-submenu" id="first">
                        <div class="nav-item"><a id="first-item" href="#first-page">First</a></div>
                        <div class="nav-submenu-items">
                            <div class="nav-item"><a id="first-link" href="#one">One</a></div>
                            <div class="nav-item"><a id="first-other" href="#two">Two</a></div>
                        </div>
                    </div>
                    <div class="nav-submenu" id="second">
                        <div class="nav-item"><a id="second-item" tabindex="0">Second</a></div>
                        <div class="nav-submenu-items">
                            <div class="nav-item"><a id="second-link" href="#three">Three</a></div>
                        </div>
                    </div>
                    <div class="nav-item"><a id="plain-link" href="#four">Four</a></div>
                </nav>
            </div>
        </div>
        <p id="away" style="margin-top: 200px;">Away</p>
        """;

    private const int HideDelay = 100;

    private const string FirstPanelLeft = """document.querySelector("#first .nav-submenu-items").getBoundingClientRect().left""";

    private async Task OpenAsync(int width = 1000)
    {
        await Page.SetViewportSizeAsync(width, 600);
        await GoToAsync(Navbar, Head);
    }

    private async Task OpenWithPanelWidthAsync(int width)
    {
        await Page.SetViewportSizeAsync(1000, 600);
        await GoToAsync(Navbar, Head.Replace(".nav-submenu-items { position: absolute; top: 2rem;", $".nav-submenu-items {{ right: 0; width: {width}px; position: absolute; top: 2rem;"));
    }

    private Task<bool> IsShownAsync(string id) => Page.EvaluateAsync<bool>($"""getComputedStyle(document.querySelector("#{id} .nav-submenu-items")).visibility === "visible" """);

    // hiding is what the delay of the panel applies to
    private async Task<bool> IsHiddenAsync(string id)
    {
        await Page.WaitForTimeoutAsync(HideDelay * 2);
        return !await IsShownAsync(id);
    }

    private Task<string> GetHashAsync() => Page.EvaluateAsync<string>("location.hash");

    private Task<string?> GetFocusedIdAsync() => Page.EvaluateAsync<string?>("document.activeElement?.id");

    private Task<bool> IsBurgerOpenAsync() => Page.EvaluateAsync<bool>("""document.querySelector(".navbar").hasAttribute("data-reu-nav-open")""");

    // a real tap needs a touch context, so this sends what the script gets to see of one
    private Task TapAsync(string id) => Page.EvaluateAsync($$"""
        const element = document.getElementById("{{id}}");
        element.dispatchEvent(new PointerEvent("pointerdown", { pointerType: "touch", bubbles: true }));
        element.dispatchEvent(new MouseEvent("click", { detail: 1, bubbles: true, cancelable: true }));
        """);

    // the burger checkbox comes first in the tab order, even when it is not shown
    private async Task TabOntoFirstItemAsync()
    {
        await Page.Locator(".nav-toggle input").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
    }

    private Task WaitForRenderAsync() => Page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");

    [Test]
    public async Task Hover_OverItem_ShowsSubmenu()
    {
        // Arrange
        await OpenAsync();

        // Act
        await Page.Locator("#first-item").HoverAsync();

        // Assert
        await Assert.That(await IsShownAsync("first")).IsTrue();
    }

    [Test]
    public async Task ClickItem_WithMouse_FollowsItsLink()
    {
        // Arrange
        await OpenAsync();

        // Act
        await Page.Locator("#first-item").ClickAsync();

        // Assert
        await Assert.That(await GetHashAsync()).IsEqualTo("#first-page");
    }

    [Test]
    public async Task ClickItem_WithMouse_HidesSubmenuUnderPointer()
    {
        // Arrange
        await OpenAsync();

        // Act
        await Page.Locator("#first-item").ClickAsync();

        // Assert
        await Assert.That(await IsHiddenAsync("first")).IsTrue();
    }

    [Test]
    public async Task ClickItem_WithoutLink_KeepsSubmenuShown()
    {
        // Arrange
        await OpenAsync();

        // Act
        await Page.Locator("#second-item").ClickAsync();

        // Assert
        await Assert.That(await IsShownAsync("second")).IsTrue();
    }

    [Test]
    public async Task ClickLink_InsideSubmenu_HidesItUnderPointer()
    {
        // Arrange
        await OpenAsync();
        await Page.Locator("#first-item").HoverAsync();

        // Act
        await Page.Locator("#first-link").ClickAsync();

        // Assert
        await Assert.That(await IsHiddenAsync("first")).IsTrue();
        await Assert.That(await GetHashAsync()).IsEqualTo("#one");
    }

    [Test]
    public async Task Hover_AfterLeavingDismissedSubmenu_ShowsItAgain()
    {
        // Arrange
        await OpenAsync();
        await Page.Locator("#first-item").ClickAsync();
        await Page.Locator("#away").HoverAsync();

        // Act
        await Page.Locator("#first-item").HoverAsync();

        // Assert
        await Assert.That(await IsShownAsync("first")).IsTrue();
    }

    [Test]
    public async Task Navigate_ResettingAttributes_KeepsSubmenuDismissed()
    {
        // Arrange
        await OpenAsync();
        await Page.Locator("#first-item").HoverAsync();
        await Page.Locator("#first-link").ClickAsync();

        // Act
        await Page.EvaluateAsync("""
            document.querySelector(".navbar").removeAttribute("data-reu-nav-mode");
            document.getElementById("first").removeAttribute("data-reu-nav-submenu");
            """);
        await Page.WaitForFunctionAsync("""document.querySelector(".navbar").hasAttribute("data-reu-nav-mode")""");

        // Assert
        await Assert.That(await IsHiddenAsync("first")).IsTrue();
    }

    [Test]
    public async Task Focus_MovingAwayWhileHovering_KeepsSubmenuDismissed()
    {
        // Arrange
        await OpenAsync();
        await Page.Locator("#first-item").ClickAsync();

        // Act
        await Page.Locator("#plain-link").FocusAsync();

        // Assert
        await Assert.That(await IsHiddenAsync("first")).IsTrue();
    }

    [Test]
    public async Task Hover_LeavingWhileFocused_KeepsSubmenuDismissed()
    {
        // Arrange
        await OpenAsync();
        await TabOntoFirstItemAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Page.Locator("#first-item").HoverAsync();

        // Act
        await Page.Locator("#away").HoverAsync();

        // Assert
        await Assert.That(await IsHiddenAsync("first")).IsTrue();
    }

    [Test]
    public async Task ShowSubmenu_InsideClosedBurger_StaysHidden()
    {
        // Act
        await OpenAsync(200);

        // Assert
        await Assert.That(await IsHiddenAsync("first")).IsTrue();
    }

    [Test]
    public async Task OpenBurger_ListsSubmenuItems()
    {
        // Arrange
        await OpenAsync(200);

        // Act
        await Page.Locator(".nav-toggle").ClickAsync();

        // Assert
        await Assert.That(await IsShownAsync("first")).IsTrue();
    }

    [Test]
    public async Task PressEscape_WhileHovering_HidesSubmenu()
    {
        // Arrange
        await OpenAsync();
        await Page.Locator("#first-item").HoverAsync();

        // Act
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await Assert.That(await IsHiddenAsync("first")).IsTrue();
    }

    [Test]
    public async Task Tab_OntoItem_ShowsSubmenu()
    {
        // Arrange
        await OpenAsync();

        // Act
        await TabOntoFirstItemAsync();

        // Assert
        await Assert.That(await GetFocusedIdAsync()).IsEqualTo("first-item");
        await Assert.That(await IsShownAsync("first")).IsTrue();
    }

    [Test]
    public async Task Tab_FromItem_EntersSubmenu()
    {
        // Arrange
        await OpenAsync();
        await TabOntoFirstItemAsync();

        // Act
        await Page.Keyboard.PressAsync("Tab");

        // Assert
        await Assert.That(await GetFocusedIdAsync()).IsEqualTo("first-link");
    }

    [Test]
    public async Task PressEscape_WithFocusInsidePanel_HidesItAndReturnsFocusToItem()
    {
        // Arrange
        await OpenAsync();
        await TabOntoFirstItemAsync();
        await Page.Keyboard.PressAsync("Tab");

        // Act
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await Assert.That(await GetFocusedIdAsync()).IsEqualTo("first-item");
        await Assert.That(await IsHiddenAsync("first")).IsTrue();
    }

    [Test]
    public async Task Tab_FromDismissedItem_SkipsItsPanel()
    {
        // Arrange
        await OpenAsync();
        await TabOntoFirstItemAsync();
        await Page.Keyboard.PressAsync("Escape");

        // Act
        await Page.Keyboard.PressAsync("Tab");

        // Assert
        await Assert.That(await GetFocusedIdAsync()).IsEqualTo("second-item");
    }

    [Test]
    public async Task ShiftTab_BackToDismissedItem_ShowsSubmenuAgain()
    {
        // Arrange
        await OpenAsync();
        await TabOntoFirstItemAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Page.Keyboard.PressAsync("Tab");

        // Act
        await Page.Keyboard.PressAsync("Shift+Tab");

        // Assert
        await Assert.That(await IsShownAsync("first")).IsTrue();
    }

    [Test]
    public async Task PressEnter_OnItem_FollowsItsLink()
    {
        // Arrange
        await OpenAsync();
        await TapAsync("plain-link");
        await Page.Locator("#first-item").FocusAsync();

        // Act
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await Assert.That(await GetHashAsync()).IsEqualTo("#first-page");
    }

    [Test]
    public async Task PressEnter_OnLinkInPanel_HidesSubmenu()
    {
        // Arrange
        await OpenAsync();
        await TabOntoFirstItemAsync();
        await Page.Keyboard.PressAsync("Tab");

        // Act
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await Assert.That(await GetFocusedIdAsync()).IsEqualTo("first-item");
        await Assert.That(await IsHiddenAsync("first")).IsTrue();
    }

    [Test]
    public async Task Tap_OnItem_ShowsSubmenuInsteadOfFollowingLink()
    {
        // Arrange
        await OpenAsync();

        // Act
        await TapAsync("first-item");

        // Assert
        await Assert.That(await IsShownAsync("first")).IsTrue();
        await Assert.That(await GetHashAsync()).IsEqualTo("");
    }

    [Test]
    public async Task Tap_OnItemAgain_FollowsLink()
    {
        // Arrange
        await OpenAsync();
        await TapAsync("first-item");

        // Act
        await TapAsync("first-item");

        // Assert
        await Assert.That(await GetHashAsync()).IsEqualTo("#first-page");
    }

    [Test]
    public async Task Tap_OnItemWithoutLinkAgain_HidesSubmenu()
    {
        // Arrange
        await OpenAsync();
        await TapAsync("second-item");

        // Act
        await TapAsync("second-item");

        // Assert
        await Assert.That(await IsHiddenAsync("second")).IsTrue();
    }

    [Test]
    public async Task Tap_OnOtherItem_SwitchesSubmenu()
    {
        // Arrange
        await OpenAsync();
        await TapAsync("first-item");

        // Act
        await TapAsync("second-item");

        // Assert
        await Assert.That(await IsHiddenAsync("first")).IsTrue();
        await Assert.That(await IsShownAsync("second")).IsTrue();
    }

    [Test]
    public async Task Tap_OnLinkInPanel_FollowsItAndHidesSubmenu()
    {
        // Arrange
        await OpenAsync();
        await TapAsync("first-item");

        // Act
        await TapAsync("first-link");

        // Assert
        await Assert.That(await GetHashAsync()).IsEqualTo("#one");
        await Assert.That(await IsHiddenAsync("first")).IsTrue();
    }

    [Test]
    public async Task Tap_OnPlainItem_FollowsLink()
    {
        // Arrange
        await OpenAsync();

        // Act
        await TapAsync("plain-link");

        // Assert
        await Assert.That(await GetHashAsync()).IsEqualTo("#four");
    }

    [Test]
    public async Task PointerDown_OutsideTappedSubmenu_HidesIt()
    {
        // Arrange
        await OpenAsync();
        await TapAsync("first-item");

        // Act
        await Page.Locator("#away").ClickAsync();

        // Assert
        await Assert.That(await IsHiddenAsync("first")).IsTrue();
    }

    [Test]
    public async Task PointerDown_InsideTappedSubmenu_KeepsItShown()
    {
        // Arrange
        await OpenAsync();
        await TapAsync("first-item");

        // Act
        await Page.EvaluateAsync("""document.getElementById("first-link").dispatchEvent(new PointerEvent("pointerdown", { pointerType: "touch", bubbles: true }))""");

        // Assert
        await Assert.That(await IsShownAsync("first")).IsTrue();
    }

    [Test]
    public async Task PressEscape_WithTappedSubmenu_HidesIt()
    {
        // Arrange
        await OpenAsync();
        await TapAsync("first-item");

        // Act
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await Assert.That(await IsHiddenAsync("first")).IsTrue();
    }

    [Test]
    public async Task Resize_ToOtherLayout_HidesTappedSubmenu()
    {
        // Arrange
        await OpenAsync();
        await TapAsync("first-item");

        // Act
        await Page.SetViewportSizeAsync(400, 600);
        await Page.WaitForFunctionAsync("""document.querySelector(".navbar").getAttribute("data-reu-nav-mode") === "icons" """);

        // Assert
        await Assert.That(await IsHiddenAsync("first")).IsTrue();
    }

    [Test]
    public async Task Resize_WithinSameLayout_KeepsTappedSubmenuShown()
    {
        // Arrange
        await OpenAsync();
        await TapAsync("first-item");

        // Act
        await Page.SetViewportSizeAsync(950, 600);
        await WaitForRenderAsync();

        // Assert
        await Assert.That(await IsShownAsync("first")).IsTrue();
    }

    [Test]
    public async Task Open_WithPanelPastTheEdge_MovesItIntoView()
    {
        // Act
        await OpenWithPanelWidthAsync(500);

        // Assert
        await Assert.That(await Page.EvaluateAsync<double>(FirstPanelLeft)).IsEqualTo(8);
    }

    [Test]
    public async Task Open_WithRoomForPanel_LeavesItInPlace()
    {
        // Act
        await OpenWithPanelWidthAsync(100);

        // Assert
        await Assert.That(await Page.EvaluateAsync<string>("""document.querySelector("#first .nav-submenu-items").style.translate""")).IsEqualTo("");
    }

    [Test]
    public async Task Resize_GivingPanelRoomAgain_MovesItBack()
    {
        // Arrange
        await OpenWithPanelWidthAsync(500);

        // Act
        await Page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = ".nav-brand { width: 700px; }" });
        await Page.SetViewportSizeAsync(1200, 600);
        await WaitForRenderAsync();

        // Assert
        await Assert.That(await Page.EvaluateAsync<string>("""document.querySelector("#first .nav-submenu-items").style.translate""")).IsEqualTo("");
    }

    [Test]
    public async Task Update_WithWideSubmenuItem_SizesBurgerForIt()
    {
        // Arrange
        await Page.SetViewportSizeAsync(200, 600);

        // Act
        await GoToAsync(Navbar.Replace("""<a id="second-link" """, """<a id="second-link" style="display: block; width: 900px;" """), Head);

        // Assert
        await Assert.That(await Page.EvaluateAsync<double>("""parseFloat(document.querySelector(".navbar").style.getPropertyValue("--reu-nav-menu-width"))""")).IsGreaterThanOrEqualTo(900);
    }

    [Test]
    public async Task ClickItem_InsideBurger_FollowsLinkAndClosesBurger()
    {
        // Arrange
        await OpenAsync(200);
        await Page.Locator(".nav-toggle").ClickAsync();

        // Act
        await Page.Locator("#first-item").ClickAsync();

        // Assert
        await Assert.That(await GetHashAsync()).IsEqualTo("#first-page");
        await Assert.That(await IsBurgerOpenAsync()).IsFalse();
    }

    [Test]
    public async Task Tap_OnItemInsideBurger_FollowsLinkAtOnce()
    {
        // Arrange
        await OpenAsync(200);
        await Page.Locator(".nav-toggle").ClickAsync();

        // Act
        await TapAsync("first-item");

        // Assert
        await Assert.That(await GetHashAsync()).IsEqualTo("#first-page");
    }

    [Test]
    public async Task ClickItem_WithoutLinkInsideBurger_KeepsBurgerOpen()
    {
        // Arrange
        await OpenAsync(200);
        await Page.Locator(".nav-toggle").ClickAsync();

        // Act
        await Page.Locator("#second-item").ClickAsync();

        // Assert
        await Assert.That(await IsBurgerOpenAsync()).IsTrue();
    }

    [Test]
    public async Task ClickLink_InsideSubmenuInBurger_ClosesBurger()
    {
        // Arrange
        await OpenAsync(200);
        await Page.Locator(".nav-toggle").ClickAsync();

        // Act
        await Page.Locator("#first-link").ClickAsync();

        // Assert
        await Assert.That(await IsBurgerOpenAsync()).IsFalse();
    }

    [Test]
    public async Task PressEscape_InsideBurger_ClosesBurger()
    {
        // Arrange
        await OpenAsync(200);
        await Page.Locator(".nav-toggle").ClickAsync();
        await Page.Locator("#first-item").HoverAsync();

        // Act
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await Assert.That(await IsBurgerOpenAsync()).IsFalse();
    }
}
