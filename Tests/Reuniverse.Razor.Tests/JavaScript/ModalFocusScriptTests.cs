namespace Reuniverse.Razor.Tests.JavaScript;

public class ModalFocusScriptTests : BrowserTestBase
{
    private const string Script = """<script src="/reuniverse.js" defer></script>""";

    private const string Buttons = """
        <button id="opener">Open</button>
        <button id="other">Other</button>
        """;

    // the hidden button is last, as only one that can be focused counts as the end of the dialog
    private const string Dialog = """
        <div id="dialog" role="dialog" aria-modal="true" tabindex="-1">
            <button id="first">First</button>
            <button id="last">Last</button>
            <button id="hidden" style="display: none;">Hidden</button>
        </div>
        """;

    private async Task LoadButtonsAsync()
    {
        await GoToAsync(Buttons, Script);
        await Page.Locator("#opener").FocusAsync();
    }

    // MutationObserver callbacks run after the current task
    private Task SettleAsync() => Page.EvaluateAsync("() => new Promise(resolve => setTimeout(resolve))");

    private async Task ShowAsync(string html, string parent = "body")
    {
        await Page.EvaluateAsync("([html, parent]) => document.querySelector(parent).insertAdjacentHTML('beforeend', html)", new[] { html, parent });
        await SettleAsync();
    }

    private async Task RemoveAsync(string selector)
    {
        await Page.EvaluateAsync("selector => document.querySelector(selector).remove()", selector);
        await SettleAsync();
    }

    private Task<string> FocusedIdAsync() => Page.EvaluateAsync<string>("() => document.activeElement?.id ?? ''");

    [Test]
    public async Task Show_WithFocusableDialog_FocusesDialog()
    {
        // Arrange
        await LoadButtonsAsync();

        // Act
        await ShowAsync(Dialog);

        // Assert
        await Assert.That(await FocusedIdAsync()).IsEqualTo("dialog");
    }

    [Test]
    public async Task Show_WithAutofocus_FocusesThatElement()
    {
        // Arrange
        await LoadButtonsAsync();

        // Act
        await ShowAsync("""<div role="dialog" aria-modal="true" tabindex="-1"><button>First</button><input id="name" autofocus /></div>""");

        // Assert
        await Assert.That(await FocusedIdAsync()).IsEqualTo("name");
    }

    [Test]
    public async Task Show_WithoutTabindex_FocusesFirstFocusable()
    {
        // Arrange
        await LoadButtonsAsync();

        // Act
        await ShowAsync("""<div role="dialog" aria-modal="true"><button id="first">First</button></div>""");

        // Assert
        await Assert.That(await FocusedIdAsync()).IsEqualTo("first");
    }

    [Test]
    public async Task Show_WithoutAriaModal_LeavesFocus()
    {
        // Arrange
        await LoadButtonsAsync();

        // Act
        await ShowAsync("""<div role="dialog" tabindex="-1"><button>First</button></div>""");

        // Assert
        await Assert.That(await FocusedIdAsync()).IsEqualTo("opener");
    }

    [Test]
    public async Task Load_WithDialogAlreadyOpen_FocusesDialog()
    {
        // Act
        await GoToAsync(Buttons + Dialog, Script);

        // Assert
        await Assert.That(await FocusedIdAsync()).IsEqualTo("dialog");
    }

    [Test]
    public async Task Tab_FromDialog_FocusesFirst()
    {
        // Arrange
        await LoadButtonsAsync();
        await ShowAsync(Dialog);

        // Act
        await Page.Keyboard.PressAsync("Tab");

        // Assert
        await Assert.That(await FocusedIdAsync()).IsEqualTo("first");
    }

    [Test]
    public async Task ShiftTab_FromDialog_FocusesLast()
    {
        // Arrange
        await LoadButtonsAsync();
        await ShowAsync(Dialog);

        // Act
        await Page.Keyboard.PressAsync("Shift+Tab");

        // Assert
        await Assert.That(await FocusedIdAsync()).IsEqualTo("last");
    }

    [Test]
    public async Task Tab_FromLast_WrapsToFirst()
    {
        // Arrange
        await LoadButtonsAsync();
        await ShowAsync(Dialog);
        await Page.Locator("#last").FocusAsync();

        // Act
        await Page.Keyboard.PressAsync("Tab");

        // Assert
        await Assert.That(await FocusedIdAsync()).IsEqualTo("first");
    }

    [Test]
    public async Task ShiftTab_FromFirst_WrapsToLast()
    {
        // Arrange
        await LoadButtonsAsync();
        await ShowAsync(Dialog);
        await Page.Locator("#first").FocusAsync();

        // Act
        await Page.Keyboard.PressAsync("Shift+Tab");

        // Assert
        await Assert.That(await FocusedIdAsync()).IsEqualTo("last");
    }

    [Test]
    public async Task Tab_WithInertContent_SkipsIt()
    {
        // Arrange
        await LoadButtonsAsync();
        await ShowAsync("""<div role="dialog" aria-modal="true" tabindex="-1"><button id="first">First</button><div inert><button>Collapsed</button></div></div>""");
        await Page.Locator("#first").FocusAsync();

        // Act
        await Page.Keyboard.PressAsync("Tab");

        // Assert
        await Assert.That(await FocusedIdAsync()).IsEqualTo("first");
    }

    [Test]
    public async Task Tab_WithNothingToFocus_KeepsFocusOnDialog()
    {
        // Arrange
        await LoadButtonsAsync();
        await ShowAsync("""<div id="dialog" role="dialog" aria-modal="true" tabindex="-1">Text only</div>""");

        // Act
        await Page.Keyboard.PressAsync("Tab");

        // Assert
        await Assert.That(await FocusedIdAsync()).IsEqualTo("dialog");
    }

    [Test]
    public async Task Tab_WithoutDialog_MovesAsUsual()
    {
        // Arrange
        await LoadButtonsAsync();

        // Act
        await Page.Keyboard.PressAsync("Tab");

        // Assert
        await Assert.That(await FocusedIdAsync()).IsEqualTo("other");
    }

    [Test]
    public async Task Remove_WithFocusInside_ReturnsFocusToOpener()
    {
        // Arrange
        await LoadButtonsAsync();
        await ShowAsync(Dialog);
        await Page.Locator("#first").FocusAsync();

        // Act
        await RemoveAsync("#dialog");

        // Assert
        await Assert.That(await FocusedIdAsync()).IsEqualTo("opener");
    }

    [Test]
    public async Task Remove_WithParent_ReturnsFocusToOpener()
    {
        // Arrange
        await LoadButtonsAsync();
        await ShowAsync($"""<div id="backdrop">{Dialog}</div>""");

        // Act
        await RemoveAsync("#backdrop");

        // Assert
        await Assert.That(await FocusedIdAsync()).IsEqualTo("opener");
    }

    [Test]
    public async Task Remove_AfterFocusMovedOut_LeavesFocus()
    {
        // Arrange
        await LoadButtonsAsync();
        await ShowAsync("""<section class="reu-update-prompt"><button id="update">Update</button></section>""");
        await ShowAsync(Dialog);
        await Page.Locator("#update").FocusAsync();

        // Act
        await RemoveAsync("#dialog");

        // Assert
        await Assert.That(await FocusedIdAsync()).IsEqualTo("update");
    }

    [Test]
    public async Task Remove_WithOpenerGone_LeavesFocus()
    {
        // Arrange
        await LoadButtonsAsync();
        await ShowAsync(Dialog);
        await Page.EvaluateAsync("() => document.getElementById('opener').remove()");

        // Act
        await RemoveAsync("#dialog");

        // Assert
        await Assert.That(await FocusedIdAsync()).IsEqualTo("");
    }

    [Test]
    public async Task Tab_WithNestedDialog_StaysInInnerDialog()
    {
        // Arrange
        await LoadButtonsAsync();
        await ShowAsync(Dialog);
        await Page.Locator("#first").FocusAsync();
        await ShowAsync("""<div id="inner" role="dialog" aria-modal="true" tabindex="-1"><button id="inner-only">Only</button></div>""", "#dialog");
        await Page.Locator("#inner-only").FocusAsync();

        // Act
        await Page.Keyboard.PressAsync("Tab");

        // Assert
        await Assert.That(await FocusedIdAsync()).IsEqualTo("inner-only");
    }

    [Test]
    public async Task Remove_WithNestedDialog_ReturnsFocusToOuterDialog()
    {
        // Arrange
        await LoadButtonsAsync();
        await ShowAsync(Dialog);
        await Page.Locator("#first").FocusAsync();
        await ShowAsync("""<div id="inner" role="dialog" aria-modal="true" tabindex="-1"><button>Only</button></div>""", "#dialog");

        // Act
        await RemoveAsync("#inner");

        // Assert
        await Assert.That(await FocusedIdAsync()).IsEqualTo("first");
    }

    private Task<bool> IsInertAsync(string selector) => Page.EvaluateAsync<bool>("selector => document.querySelector(selector).inert", selector);

    [Test]
    public async Task Show_WithDialog_MakesRestOfPageInert()
    {
        // Arrange
        await LoadButtonsAsync();

        // Act
        await ShowAsync(Dialog);

        // Assert
        await Assert.That(await IsInertAsync("#opener")).IsTrue();
        await Assert.That(await IsInertAsync("#other")).IsTrue();
        await Assert.That(await IsInertAsync("#dialog")).IsFalse();
    }

    [Test]
    public async Task Show_DeepInPage_KeepsElementsItIsInActive()
    {
        // Arrange
        await GoToAsync("""<main id="main"><aside id="aside">Aside</aside><div id="host"></div></main><footer id="footer">Footer</footer>""", Script);

        // Act
        await ShowAsync($"""<div id="backdrop">{Dialog}</div>""", "#host");

        // Assert
        await Assert.That(await IsInertAsync("#main")).IsFalse();
        await Assert.That(await IsInertAsync("#host")).IsFalse();
        await Assert.That(await IsInertAsync("#backdrop")).IsFalse();
        await Assert.That(await IsInertAsync("#aside")).IsTrue();
        await Assert.That(await IsInertAsync("#footer")).IsTrue();
    }

    [Test]
    public async Task Focus_BehindOpenDialog_StaysInDialog()
    {
        // Arrange
        await LoadButtonsAsync();
        await ShowAsync(Dialog);

        // Act
        await Page.EvaluateAsync("() => document.getElementById('other').focus()");

        // Assert
        await Assert.That(await FocusedIdAsync()).IsEqualTo("dialog");
    }

    [Test]
    public async Task Show_WithPanelsOfThisScript_KeepsThemUsable()
    {
        // Arrange
        await GoToAsync("""
            <button id="opener">Open</button>
            <div id="blazor-error-ui">Error</div>
            <section class="reu-error-panel" id="errors">Errors</section>
            <section class="reu-update-prompt" id="update">Update</section>
            """, Script);

        // Act
        await ShowAsync(Dialog);

        // Assert
        await Assert.That(await IsInertAsync("#opener")).IsTrue();
        await Assert.That(await IsInertAsync("#blazor-error-ui")).IsFalse();
        await Assert.That(await IsInertAsync("#errors")).IsFalse();
        await Assert.That(await IsInertAsync("#update")).IsFalse();
    }

    [Test]
    public async Task Remove_Dialog_MakesPageActiveAgain()
    {
        // Arrange
        await LoadButtonsAsync();
        await ShowAsync(Dialog);

        // Act
        await RemoveAsync("#dialog");

        // Assert
        await Assert.That(await IsInertAsync("#opener")).IsFalse();
        await Assert.That(await IsInertAsync("#other")).IsFalse();
    }

    [Test]
    public async Task Remove_Dialog_KeepsWhatWasInertBefore()
    {
        // Arrange
        await GoToAsync("""<button id="opener">Open</button><div id="already" inert>Already inert</div>""", Script);
        await ShowAsync(Dialog);

        // Act
        await RemoveAsync("#dialog");

        // Assert
        await Assert.That(await IsInertAsync("#already")).IsTrue();
        await Assert.That(await IsInertAsync("#opener")).IsFalse();
    }

    [Test]
    public async Task Show_NestedDialog_MakesOuterDialogContentInert()
    {
        // Arrange
        await LoadButtonsAsync();
        await ShowAsync(Dialog);

        // Act
        await ShowAsync("""<div id="inner" role="dialog" aria-modal="true" tabindex="-1"><button>Only</button></div>""", "#dialog");

        // Assert
        await Assert.That(await IsInertAsync("#first")).IsTrue();
        await Assert.That(await IsInertAsync("#inner")).IsFalse();
    }

    [Test]
    public async Task Remove_NestedDialog_MakesOnlyOuterDialogActiveAgain()
    {
        // Arrange
        await LoadButtonsAsync();
        await ShowAsync(Dialog);
        await ShowAsync("""<div id="inner" role="dialog" aria-modal="true" tabindex="-1"><button>Only</button></div>""", "#dialog");

        // Act
        await RemoveAsync("#inner");

        // Assert
        await Assert.That(await IsInertAsync("#first")).IsFalse();
        await Assert.That(await IsInertAsync("#other")).IsTrue();
    }

    [Test]
    public async Task Remove_BothDialogsAtOnce_MakesPageActiveAgain()
    {
        // Arrange
        await LoadButtonsAsync();
        await ShowAsync(Dialog);
        await ShowAsync("""<div id="inner" role="dialog" aria-modal="true" tabindex="-1"><button>Only</button></div>""", "#dialog");

        // Act
        await RemoveAsync("#dialog");

        // Assert
        await Assert.That(await IsInertAsync("#opener")).IsFalse();
        await Assert.That(await IsInertAsync("#other")).IsFalse();
    }

    [Test]
    public async Task Show_WithoutAriaModal_LeavesPageActive()
    {
        // Arrange
        await LoadButtonsAsync();

        // Act
        await ShowAsync("""<div id="plain" role="dialog"><button>Only</button></div>""");

        // Assert
        await Assert.That(await IsInertAsync("#other")).IsFalse();
    }

    [Test]
    public async Task Show_WithAlertDialog_TakesFocusAndMakesPageInert()
    {
        // Arrange
        await LoadButtonsAsync();

        // Act
        await ShowAsync("""<div id="alert" role="alertdialog" aria-modal="true" tabindex="-1"><button id="cancel" autofocus>Cancel</button><button>OK</button></div>""");

        // Assert
        await Assert.That(await FocusedIdAsync()).IsEqualTo("cancel");
        await Assert.That(await IsInertAsync("#other")).IsTrue();
    }
}
