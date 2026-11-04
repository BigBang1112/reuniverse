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
        await ShowAsync(Dialog);
        await Page.Locator("#other").FocusAsync();

        // Act
        await RemoveAsync("#dialog");

        // Assert
        await Assert.That(await FocusedIdAsync()).IsEqualTo("other");
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
}
