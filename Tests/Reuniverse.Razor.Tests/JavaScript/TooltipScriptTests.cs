using Microsoft.Playwright;

namespace Reuniverse.Razor.Tests.JavaScript;

public class TooltipScriptTests : BrowserTestBase
{
    private const int ShowDelay = 500;

    private const string Scripts = """
        <link rel="stylesheet" href="/reuniverse.css" />
        <script src="/reuniverse.js" defer></script>
        """;

    private const string Target = """<button id="target" style="position: absolute; left: 50px; top: 50px; width: 300px; height: 100px;" data-reu-tooltip="Hint">Target</button>""";
    private const string Other = """<button id="other" style="position: absolute; left: 50px; top: 200px; width: 300px; height: 100px;" data-reu-tooltip="Other hint">Other</button>""";
    private const string Plain = """<p id="plain" style="position: absolute; left: 50px; top: 350px;">No tooltip</p>""";

    private ILocator Tooltip => Page.Locator(TooltipSelector);

    private async Task OpenAsync(string body)
    {
        await GoToWithPausedClockAsync(body, Scripts);
    }

    private async Task MoveToAsync(string selector)
    {
        var box = await Page.Locator(selector).BoundingBoxAsync();
        await Page.Mouse.MoveAsync(box!.X + box.Width / 2, box.Y + box.Height / 2);
    }

    private Task WaitAsync(int milliseconds = ShowDelay) => Page.Clock.RunForAsync(milliseconds);

    private async Task HoverAsync(string selector)
    {
        await MoveToAsync(selector);
        await WaitAsync();
    }

    private Task<int> CountAsync() => Tooltip.CountAsync();

    private Task EvaluateAsync(string expression) => Page.EvaluateAsync(expression);

    // MutationObserver callbacks run after the current task
    private Task SettleAsync() => Page.EvaluateAsync("() => Promise.resolve()");

    private Task LeaveAsync() => MoveToAsync("#plain");

    [Test]
    public async Task Load_ByDefault_ShowsNothing()
    {
        // Act
        await OpenAsync(Target);

        // Assert
        await Assert.That(await CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Hover_BeforeDelay_ShowsNothing()
    {
        // Arrange
        await OpenAsync(Target);

        // Act
        await MoveToAsync("#target");
        await WaitAsync(ShowDelay - 50);

        // Assert
        await Assert.That(await CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Hover_AfterDelay_ShowsText()
    {
        // Arrange
        await OpenAsync(Target);

        // Act
        await HoverAsync("#target");

        // Assert
        await Assert.That(await Tooltip.TextContentAsync()).IsEqualTo("Hint");
    }

    [Test]
    public async Task Hover_WhileMovingInsideElement_KeepsWaiting()
    {
        // Arrange
        await OpenAsync(Target);
        await Page.Mouse.MoveAsync(100, 100);
        await WaitAsync(ShowDelay - 100);

        // Act
        await Page.Mouse.MoveAsync(120, 110);
        await WaitAsync(100);

        // Assert
        await Assert.That(await CountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task Hover_AfterMovingInsideElement_AppearsAtLastPosition()
    {
        // Arrange
        await OpenAsync(Target);
        await Page.Mouse.MoveAsync(100, 100);

        // Act
        await Page.Mouse.MoveAsync(120, 110);
        await WaitAsync();

        // Assert
        await Assert.That(await GetStyleNumberAsync(TooltipSelector, "left")).IsEqualTo(120);
    }

    [Test]
    public async Task Hover_PassingOverElement_ShowsNothing()
    {
        // Arrange
        await OpenAsync(Target + Plain);
        await MoveToAsync("#target");
        await WaitAsync(100);

        // Act
        await LeaveAsync();
        await WaitAsync();

        // Assert
        await Assert.That(await CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Hover_ToOtherElement_RestartsWait()
    {
        // Arrange
        await OpenAsync(Target + Other);
        await MoveToAsync("#target");
        await WaitAsync(ShowDelay - 100);

        // Act
        await MoveToAsync("#other");
        await WaitAsync(200);

        // Assert
        await Assert.That(await CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Hover_ToOtherElementAfterDelay_ShowsItsText()
    {
        // Arrange
        await OpenAsync(Target + Other);
        await HoverAsync("#target");

        // Act
        await HoverAsync("#other");

        // Assert
        await Assert.That(await Tooltip.TextContentAsync()).IsEqualTo("Other hint");
    }

    [Test]
    public async Task Leave_WhenShown_HidesWithoutDelay()
    {
        // Arrange
        await OpenAsync(Target + Plain);
        await HoverAsync("#target");

        // Act
        await LeaveAsync();

        // Assert
        await Assert.That(await CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Hover_WhenShown_FollowsCursor()
    {
        // Arrange
        await OpenAsync(Target);
        await HoverAsync("#target");

        // Act
        await Page.Mouse.MoveAsync(100, 120);

        // Assert
        await Assert.That(await GetStyleNumberAsync(TooltipSelector, "left")).IsEqualTo(100);
        await Assert.That(await GetStyleNumberAsync(TooltipSelector, "top")).IsEqualTo(136);
    }

    [Test]
    public async Task Hover_ChildOfTarget_ShowsParentText()
    {
        // Arrange
        await OpenAsync("""<div data-reu-tooltip="Parent hint"><span id="child">Child</span></div>""");

        // Act
        await HoverAsync("#child");

        // Assert
        await Assert.That(await Tooltip.TextContentAsync()).IsEqualTo("Parent hint");
    }

    [Test]
    public async Task Hover_NestedTargets_ShowsClosestText()
    {
        // Arrange
        await OpenAsync("""<div data-reu-tooltip="Parent hint"><span id="child" data-reu-tooltip="Child hint">Child</span></div>""");

        // Act
        await HoverAsync("#child");

        // Assert
        await Assert.That(await Tooltip.TextContentAsync()).IsEqualTo("Child hint");
    }

    [Test]
    public async Task Hover_EmptyText_ShowsNothing()
    {
        // Arrange
        await OpenAsync("""<button id="target" data-reu-tooltip="">Target</button>""");

        // Act
        await HoverAsync("#target");

        // Assert
        await Assert.That(await CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Hover_ElementAddedLater_ShowsText()
    {
        // Arrange
        await OpenAsync("");
        await EvaluateAsync("""document.body.insertAdjacentHTML("beforeend", '<button id="late" data-reu-tooltip="Late hint">Late</button>')""");

        // Act
        await HoverAsync("#late");

        // Assert
        await Assert.That(await Tooltip.TextContentAsync()).IsEqualTo("Late hint");
    }

    [Test]
    public async Task Hover_NearRightEdge_StaysInsideWindow()
    {
        // Arrange
        await OpenAsync("""<div id="full" style="position: fixed; inset: 0;" data-reu-tooltip="Hint"></div>""");

        // Act
        await Page.Mouse.MoveAsync(995, 100);
        await WaitAsync();

        // Assert
        var box = await Tooltip.BoundingBoxAsync();
        await Assert.That(box!.X + box.Width).IsLessThanOrEqualTo(1000);
    }

    [Test]
    public async Task Hover_NearBottomEdge_FlipsAboveCursor()
    {
        // Arrange
        await OpenAsync("""<div id="full" style="position: fixed; inset: 0;" data-reu-tooltip="Hint"></div>""");

        // Act
        await Page.Mouse.MoveAsync(100, 695);
        await WaitAsync();

        // Assert
        var box = await Tooltip.BoundingBoxAsync();
        await Assert.That(box!.Y + box.Height).IsLessThanOrEqualTo(695);
    }

    [Test]
    public async Task Hover_LongText_WrapsInsideMaximumWidth()
    {
        // Arrange
        var text = string.Join(' ', Enumerable.Repeat("long words", 30));
        await OpenAsync($"""<button id="target" data-reu-tooltip="{text}">Target</button>""");

        // Act
        await HoverAsync("#target");

        // Assert
        var box = await Tooltip.BoundingBoxAsync();
        var rootFontSize = await Page.EvaluateAsync<float>("parseFloat(getComputedStyle(document.documentElement).fontSize)");
        await Assert.That(box!.Width).IsLessThanOrEqualTo(20 * rootFontSize + 0.5f);
    }

    [Test]
    public async Task Hover_TextWithLineBreaks_KeepsLineBreaks()
    {
        // Arrange
        await OpenAsync("""<button id="target" data-reu-tooltip="First&#10;Second">Target</button>""");

        // Act
        await HoverAsync("#target");

        // Assert
        await Assert.That(await Tooltip.EvaluateAsync<string>("element => getComputedStyle(element).whiteSpace")).IsEqualTo("pre-line");
    }

    [Test]
    public async Task Hover_InsideOverflowContainer_IsNotClipped()
    {
        // Arrange
        await OpenAsync("""<div id="box" style="width: 120px; height: 40px; overflow: auto;"><button id="target" data-reu-tooltip="A hint wider than its container">Target</button></div>""");

        // Act
        await HoverAsync("#target");

        // Assert
        var tooltip = await Tooltip.BoundingBoxAsync();
        var container = await Page.Locator("#box").BoundingBoxAsync();
        await Assert.That(tooltip!.X + tooltip.Width).IsGreaterThan(container!.X + container.Width);
    }

    [Test]
    public async Task Hover_Tooltip_DoesNotTakePointerEvents()
    {
        // Arrange
        await OpenAsync(Target);

        // Act
        await HoverAsync("#target");

        // Assert
        await Assert.That(await Tooltip.EvaluateAsync<string>("element => getComputedStyle(element).pointerEvents")).IsEqualTo("none");
    }

    [Test]
    public async Task Hover_RepeatedlyAcrossElements_KeepsOneTooltip()
    {
        // Arrange
        await OpenAsync(Target + Other);
        await HoverAsync("#target");

        // Act
        await HoverAsync("#other");

        // Assert
        await Assert.That(await CountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task Hover_DisabledButton_ShowsText()
    {
        // Arrange
        await OpenAsync("""<button id="target" disabled data-reu-tooltip="Disabled hint">Target</button>""");

        // Act
        await HoverAsync("#target");

        // Assert
        await Assert.That(await Tooltip.TextContentAsync()).IsEqualTo("Disabled hint");
    }

    [Test]
    public async Task Leave_DisabledButton_Hides()
    {
        // Arrange
        await OpenAsync("""<button id="target" disabled data-reu-tooltip="Disabled hint">Target</button>""" + Plain);
        await HoverAsync("#target");

        // Act
        await LeaveAsync();

        // Assert
        await Assert.That(await CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Click_ButtonDisablingItself_DoesNotStayVisibleAfterLeaving()
    {
        // Arrange
        await OpenAsync("""<button id="target" onclick="this.disabled = true" data-reu-tooltip="Hint">Target</button>""" + Plain);
        await HoverAsync("#target");
        await Page.Mouse.DownAsync();
        await Page.Mouse.UpAsync();

        // Act
        await LeaveAsync();

        // Assert
        await Assert.That(await CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Click_ButtonRemovingItself_HidesUnderStillCursor()
    {
        // Arrange
        await OpenAsync("""<button id="target" onclick="this.remove()" data-reu-tooltip="Hint">Target</button>""");
        await HoverAsync("#target");

        // Act
        await Page.Mouse.DownAsync();
        await Page.Mouse.UpAsync();
        await SettleAsync();

        // Assert
        await Assert.That(await CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Remove_WhileWaiting_ShowsNothing()
    {
        // Arrange
        await OpenAsync(Target);
        await MoveToAsync("#target");
        await WaitAsync(100);

        // Act
        await EvaluateAsync("""document.getElementById("target").remove()""");
        await WaitAsync();

        // Assert
        await Assert.That(await CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task SetAttribute_WhenShown_UpdatesText()
    {
        // Arrange
        await OpenAsync(Target);
        await HoverAsync("#target");

        // Act
        await EvaluateAsync("""document.getElementById("target").setAttribute("data-reu-tooltip", "Changed")""");
        await SettleAsync();

        // Assert
        await Assert.That(await Tooltip.TextContentAsync()).IsEqualTo("Changed");
    }

    [Test]
    public async Task RemoveAttribute_WhenShown_Hides()
    {
        // Arrange
        await OpenAsync(Target);
        await HoverAsync("#target");

        // Act
        await EvaluateAsync("""document.getElementById("target").removeAttribute("data-reu-tooltip")""");
        await SettleAsync();

        // Assert
        await Assert.That(await CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task ReplacePageContent_WhenShown_Hides()
    {
        // Arrange
        await OpenAsync(Target);
        await HoverAsync("#target");

        // Act
        await EvaluateAsync("""document.body.innerHTML = '<button id="next" data-reu-tooltip="Next hint">Next</button>'""");
        await SettleAsync();

        // Assert
        await Assert.That(await CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task ReplacePageContent_LikeEnhancedNavigation_KeepsWorking()
    {
        // Arrange
        await OpenAsync(Target);
        await HoverAsync("#target");
        await EvaluateAsync("""document.body.innerHTML = '<button id="next" style="margin: 200px;" data-reu-tooltip="Next hint">Next</button>'""");
        await SettleAsync();

        // Act
        await HoverAsync("#next");

        // Assert
        await Assert.That(await Tooltip.TextContentAsync()).IsEqualTo("Next hint");
    }

    [Test]
    public async Task ReplaceBody_LikeLeavingInteractivePage_KeepsWorking()
    {
        // Arrange
        await OpenAsync(Target);
        await HoverAsync("#target");
        await EvaluateAsync("""
            (() => {
                const body = document.createElement("body");
                body.innerHTML = '<button id="next" style="margin: 200px;" data-reu-tooltip="Next hint">Next</button>';
                document.documentElement.replaceChild(body, document.body);
            })()
            """);
        await SettleAsync();

        // Act
        await HoverAsync("#next");

        // Assert
        await Assert.That(await Tooltip.TextContentAsync()).IsEqualTo("Next hint");
    }

    [Test]
    public async Task PointerMove_FromTouch_ShowsNothing()
    {
        // Arrange
        await OpenAsync(Target);

        // Act
        await EvaluateAsync("""document.getElementById("target").dispatchEvent(new PointerEvent("pointermove", { bubbles: true, pointerType: "touch" }))""");
        await WaitAsync();

        // Assert
        await Assert.That(await CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Focus_WithKeyboardBeforeDelay_ShowsNothing()
    {
        // Arrange
        await OpenAsync(Target);

        // Act
        await Page.Keyboard.PressAsync("Tab");
        await WaitAsync(ShowDelay - 50);

        // Assert
        await Assert.That(await CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Focus_WithKeyboard_ShowsBelowElement()
    {
        // Arrange
        await OpenAsync(Target);

        // Act
        await Page.Keyboard.PressAsync("Tab");
        await WaitAsync();

        // Assert
        var tooltip = await Tooltip.BoundingBoxAsync();
        var target = await Page.Locator("#target").BoundingBoxAsync();
        await Assert.That(tooltip!.Y).IsGreaterThanOrEqualTo(target!.Y + target.Height);
    }

    [Test]
    public async Task Focus_WithMouse_DoesNotCountAsKeyboardFocus()
    {
        // Arrange
        await OpenAsync("""<input id="target" data-reu-tooltip="Hint" />""" + Plain);
        await MoveToAsync("#target");
        await Page.Mouse.DownAsync();
        await Page.Mouse.UpAsync();

        // Act
        await LeaveAsync();
        await WaitAsync();

        // Assert
        await Assert.That(await CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Blur_WhenShown_Hides()
    {
        // Arrange
        await OpenAsync(Target);
        await Page.Keyboard.PressAsync("Tab");
        await WaitAsync();

        // Act
        await EvaluateAsync("""document.getElementById("target").blur()""");

        // Assert
        await Assert.That(await CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Show_ForScreenReaders_LinksDescription()
    {
        // Arrange
        await OpenAsync(Target);

        // Act
        await HoverAsync("#target");

        // Assert
        await Assert.That(await Page.Locator("#target").GetAttributeAsync("aria-describedby")).IsEqualTo("reu-tooltip");
        await Assert.That(await Tooltip.GetAttributeAsync("role")).IsEqualTo("tooltip");
    }

    [Test]
    public async Task Hide_AfterShowing_RemovesDescription()
    {
        // Arrange
        await OpenAsync(Target + Plain);
        await HoverAsync("#target");

        // Act
        await LeaveAsync();

        // Assert
        await Assert.That(await Page.Locator("#target").GetAttributeAsync("aria-describedby")).IsNull();
    }

    [Test]
    public async Task Show_WithOwnDescription_KeepsIt()
    {
        // Arrange
        await OpenAsync("""<button id="target" aria-describedby="own" data-reu-tooltip="Hint">Target</button>""" + Plain);
        await HoverAsync("#target");

        // Act
        await LeaveAsync();

        // Assert
        await Assert.That(await Page.Locator("#target").GetAttributeAsync("aria-describedby")).IsEqualTo("own");
    }

    [Test]
    public async Task Escape_WhenShown_Hides()
    {
        // Arrange
        await OpenAsync(Target);
        await HoverAsync("#target");

        // Act
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await Assert.That(await CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Escape_WhileWaiting_CancelsTooltip()
    {
        // Arrange
        await OpenAsync(Target);
        await MoveToAsync("#target");
        await WaitAsync(100);

        // Act
        await Page.Keyboard.PressAsync("Escape");
        await WaitAsync();

        // Assert
        await Assert.That(await CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Hover_AfterEscapeOnSameElement_StaysDismissed()
    {
        // Arrange
        await OpenAsync(Target);
        await HoverAsync("#target");
        await Page.Keyboard.PressAsync("Escape");

        // Act
        await Page.Mouse.MoveAsync(100, 100);
        await WaitAsync();

        // Assert
        await Assert.That(await CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Hover_AfterEscapeAndLeaving_ShowsAgain()
    {
        // Arrange
        await OpenAsync(Target + Plain);
        await HoverAsync("#target");
        await Page.Keyboard.PressAsync("Escape");
        await LeaveAsync();

        // Act
        await HoverAsync("#target");

        // Assert
        await Assert.That(await CountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task Scroll_WhenShown_Hides()
    {
        // Arrange
        await OpenAsync(Target);
        await HoverAsync("#target");

        // Act
        await EvaluateAsync("""document.dispatchEvent(new Event("scroll"))""");

        // Assert
        await Assert.That(await CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Scroll_WhileWaiting_CancelsTooltip()
    {
        // Arrange
        await OpenAsync(Target);
        await MoveToAsync("#target");
        await WaitAsync(100);

        // Act
        await EvaluateAsync("""document.dispatchEvent(new Event("scroll"))""");
        await WaitAsync();

        // Assert
        await Assert.That(await CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task PointerLeave_FromWindow_Hides()
    {
        // Arrange
        await OpenAsync(Target);
        await HoverAsync("#target");

        // Act
        await EvaluateAsync("""document.documentElement.dispatchEvent(new PointerEvent("pointerleave"))""");

        // Assert
        await Assert.That(await CountAsync()).IsEqualTo(0);
    }
}
