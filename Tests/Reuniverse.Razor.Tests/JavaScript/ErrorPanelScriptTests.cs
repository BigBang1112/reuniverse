using Microsoft.Playwright;

namespace Reuniverse.Razor.Tests.JavaScript;

public class ErrorPanelScriptTests : BrowserTestBase
{
    private const string Script = """<script src="/reuniverse.js" defer></script>""";

    private const string BlazorErrorUi = """
        <div id="blazor-error-ui" style="display: none;">
            An unhandled error has occurred.
            <a href="." class="reload">Reload</a>
            <span class="dismiss">🗙</span>
        </div>
        """;

    private const string PanelSelector = ".reu-error-panel";
    private const string LogSelector = ".reu-error-log";
    private const string ReloadSelector = ".reu-error-reload";
    private const string DismissSelector = ".reu-error-dismiss";
    private const string CopySelector = ".reu-error-copy";

    private Task OpenAsync() => GoToAsync(BlazorErrorUi, Script);

    private Task LogErrorAsync(string message) => Page.EvaluateAsync("message => console.error(message)", message);

    // the same as Blazor does it, which is all it does to report an error
    private async Task RevealBlazorErrorAsync()
    {
        await Page.EvaluateAsync("""document.getElementById("blazor-error-ui").style.display = "block" """);
        await WaitForPanelAsync();
    }

    private async Task FailAsync(string message)
    {
        await LogErrorAsync(message);
        await RevealBlazorErrorAsync();
    }

    private async Task WaitForPanelAsync()
    {
        await Page.WaitForSelectorAsync(PanelSelector, new PageWaitForSelectorOptions { State = WaitForSelectorState.Attached });
    }

    private Task<bool> IsPanelOpenAsync() => Page.Locator(PanelSelector).IsVisibleAsync();

    private Task<string> GetLogAsync() => Page.EvalOnSelectorAsync<string>(LogSelector, "log => log.textContent");

    private Task<bool> IsDetailsOpenAsync() => Page.EvalOnSelectorAsync<bool>(".reu-error-details", "details => details.open");

    private Task WaitForCopyLabelAsync(string label) => Page.WaitForFunctionAsync("label => document.querySelector('.reu-error-copy').textContent === label", label);

    private Task WaitForLogAsync(string text) => Page.WaitForFunctionAsync("text => document.querySelector('.reu-error-log').textContent.includes(text)", text);

    [Test]
    public async Task Load_WithoutError_DoesNotShowPanel()
    {
        // Arrange & Act
        await OpenAsync();

        // Assert
        await Assert.That(await Page.Locator(PanelSelector).CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task ConsoleError_WithoutBlazorError_DoesNotShowPanel()
    {
        // Arrange
        await OpenAsync();

        // Act
        await LogErrorAsync("not fatal");

        // Assert
        await Assert.That(await Page.Locator(PanelSelector).CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task ConsoleError_StillReachesConsole()
    {
        // Arrange
        await OpenAsync();

        // Act
        var message = await Page.RunAndWaitForConsoleMessageAsync(() => LogErrorAsync("to the console"));

        // Assert
        await Assert.That(message.Text).IsEqualTo("to the console");
    }

    [Test]
    public async Task BlazorError_ShowsPanelWithLog()
    {
        // Arrange
        await OpenAsync();

        // Act
        await FailAsync("Unhandled exception rendering component");

        // Assert
        await Assert.That(await IsPanelOpenAsync()).IsTrue();
        await Assert.That(await GetLogAsync()).Contains("Unhandled exception rendering component");
        await Assert.That(await IsDetailsOpenAsync()).IsTrue();
    }

    [Test]
    public async Task BlazorError_HidesBlazorErrorUi()
    {
        // Arrange
        await OpenAsync();

        // Act
        await FailAsync("boom");

        // Assert
        await Assert.That(await Page.Locator("#blazor-error-ui").IsVisibleAsync()).IsFalse();
    }

    [Test]
    public async Task BlazorError_WithoutAnyLoggedError_ShowsPanelWithoutLog()
    {
        // Arrange
        await OpenAsync();

        // Act
        await RevealBlazorErrorAsync();

        // Assert
        await Assert.That(await IsPanelOpenAsync()).IsTrue();
        await Assert.That(await Page.Locator(".reu-error-details").IsVisibleAsync()).IsFalse();
        await Assert.That(await Page.Locator(CopySelector).IsVisibleAsync()).IsFalse();
    }

    [Test]
    public async Task BlazorError_IncludesErrorsLoggedBefore()
    {
        // Arrange
        await OpenAsync();
        await LogErrorAsync("first");

        // Act
        await FailAsync("second");

        // Assert
        await Assert.That(await GetLogAsync()).Contains("first");
        await Assert.That(await GetLogAsync()).Contains("second");
    }

    [Test]
    public async Task ConsoleError_WhilePanelIsOpen_AppendsToLog()
    {
        // Arrange
        await OpenAsync();
        await FailAsync("first");

        // Act
        await LogErrorAsync("second");

        // Assert
        await Assert.That(await GetLogAsync()).Contains("second");
    }

    [Test]
    public async Task ConsoleError_WithError_LogsItsStack()
    {
        // Arrange
        await OpenAsync();

        // Act
        await Page.EvaluateAsync("""
            () => {
                function failingFunction() {
                    console.error(new Error("with stack"));
                }

                failingFunction();
            }
            """);
        await RevealBlazorErrorAsync();

        // Assert
        await Assert.That(await GetLogAsync()).Contains("with stack");
        await Assert.That(await GetLogAsync()).Contains("failingFunction");
    }

    [Test]
    public async Task ConsoleError_WithValueThatCannotBeDescribed_DoesNotThrow()
    {
        // Arrange
        await OpenAsync();

        // Act
        var message = await Page.EvaluateAsync<string>("""
            () => {
                const circular = {};
                circular.self = circular;
                console.error(circular);
                return "survived";
            }
            """);
        await RevealBlazorErrorAsync();

        // Assert
        await Assert.That(message).IsEqualTo("survived");
        await Assert.That(await GetLogAsync()).Contains("[object Object]");
    }

    [Test]
    public async Task ConsoleError_WithMarkup_ShowsItAsText()
    {
        // Arrange
        await OpenAsync();

        // Act
        await FailAsync("""<img src=x onerror="window.injected = true">""");

        // Assert
        await Assert.That(await Page.Locator($"{LogSelector} img").CountAsync()).IsEqualTo(0);
        await Assert.That(await GetLogAsync()).Contains("<img src=x");
    }

    [Test]
    public async Task ConsoleError_PastTheLimit_DropsOldestEntries()
    {
        // Arrange
        await OpenAsync();

        // Act
        await Page.EvaluateAsync("""
            () => {
                for (let i = 0; i < 150; i++) {
                    console.error(`entry-${i}-end`);
                }
            }
            """);
        await RevealBlazorErrorAsync();

        // Assert
        await Assert.That(await Page.Locator(".reu-error-entry").CountAsync()).IsEqualTo(100);
        await Assert.That(await GetLogAsync()).DoesNotContain("entry-49-end");
        await Assert.That(await GetLogAsync()).Contains("entry-50-end");
        await Assert.That(await GetLogAsync()).Contains("entry-149-end");
    }

    [Test]
    public async Task UncaughtError_IsLogged()
    {
        // Arrange
        await OpenAsync();
        await RevealBlazorErrorAsync();

        // Act
        await Page.EvaluateAsync("""setTimeout(() => { throw new Error("uncaught"); })""");
        await WaitForLogAsync("uncaught");

        // Assert
        await Assert.That(await GetLogAsync()).Contains("uncaught");
    }

    [Test]
    public async Task UnhandledRejection_IsLogged()
    {
        // Arrange
        await OpenAsync();
        await RevealBlazorErrorAsync();

        // Act
        await Page.EvaluateAsync("""() => { Promise.reject(new Error("rejected")); }""");
        await WaitForLogAsync("rejected");

        // Assert
        await Assert.That(await GetLogAsync()).Contains("rejected");
    }

    [Test]
    public async Task BlazorError_AfterDismiss_ShowsPanelAgain()
    {
        // Arrange
        await OpenAsync();
        await FailAsync("first");
        await Page.ClickAsync(DismissSelector);

        // Act
        await LogErrorAsync("second");
        await Page.EvaluateAsync("""document.getElementById("blazor-error-ui").style.display = "block" """);
        await WaitForPanelAsync();

        // Assert
        await Assert.That(await GetLogAsync()).Contains("second");
        await Assert.That(await GetLogAsync()).DoesNotContain("first");
    }

    [Test]
    public async Task Dismiss_RemovesPanel()
    {
        // Arrange
        await OpenAsync();
        await FailAsync("boom");

        // Act
        await Page.ClickAsync(DismissSelector);

        // Assert
        await Assert.That(await Page.Locator(PanelSelector).CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Reload_DoesNotShowPanelAgain()
    {
        // Arrange
        await OpenAsync();
        await FailAsync("boom");

        // Act
        await Page.RunAndWaitForNavigationAsync(() => Page.ClickAsync(ReloadSelector));

        // Assert
        await Assert.That(await Page.Locator(PanelSelector).CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Copy_WithClipboard_CopiesLogAndConfirms()
    {
        // Arrange
        // the real clipboard is not available on the insecure origin the tests run on
        await Page.AddInitScriptAsync("""
            Object.defineProperty(navigator, "clipboard", {
                value: { writeText: async text => { window.copiedText = text; } }
            })
            """);
        await GoToWithPausedClockAsync(BlazorErrorUi, Script);
        await FailAsync("copy me");

        // Act
        await Page.ClickAsync(CopySelector);
        await WaitForCopyLabelAsync("Copied");

        // Assert
        await Assert.That(await Page.EvaluateAsync<string>("window.copiedText")).Contains("copy me");
    }

    [Test]
    public async Task Copy_AfterConfirming_RestoresLabel()
    {
        // Arrange
        await Page.AddInitScriptAsync("""Object.defineProperty(navigator, "clipboard", { value: { writeText: async () => { } } })""");
        await GoToWithPausedClockAsync(BlazorErrorUi, Script);
        await FailAsync("copy me");
        await Page.ClickAsync(CopySelector);
        await WaitForCopyLabelAsync("Copied");

        // Act
        await Page.Clock.RunForAsync(1500);

        // Assert
        await WaitForCopyLabelAsync("Copy");
    }

    [Test]
    public async Task Copy_WithoutClipboard_SelectsLog()
    {
        // Arrange
        await Page.AddInitScriptAsync("""Object.defineProperty(navigator, "clipboard", { value: undefined })""");
        await OpenAsync();
        await FailAsync("select me");

        // Act
        await Page.ClickAsync(CopySelector);
        await Page.WaitForFunctionAsync("getSelection().toString().length > 0");

        // Assert
        await Assert.That(await Page.EvaluateAsync<string>("getSelection().toString()")).Contains("select me");
    }

    [Test]
    public async Task ReplaceBody_LikeEnhancedNavigation_KeepsPanelOpen()
    {
        // Arrange
        await OpenAsync();
        await FailAsync("boom");

        // Act
        await Page.EvaluateAsync("document.body.replaceChildren()");
        await WaitForPanelAsync();

        // Assert
        await Assert.That(await IsPanelOpenAsync()).IsTrue();
    }

    [Test]
    public async Task BlazorError_RenderedAfterLoad_ShowsPanel()
    {
        // Arrange
        await GoToAsync("", Script);
        await LogErrorAsync("late element");

        // Act
        await Page.EvaluateAsync($$"""
            () => {
                document.body.insertAdjacentHTML("beforeend", `{{BlazorErrorUi}}`);
            }
            """);
        await RevealBlazorErrorAsync();

        // Assert
        await Assert.That(await GetLogAsync()).Contains("late element");
    }

    [Test]
    public async Task BlazorError_WithLongLine_OverflowsLogHorizontally()
    {
        // Arrange
        await GoToAsync(BlazorErrorUi, $"""{Script}<link rel="stylesheet" href="/reuniverse.css" />""");
        await LogErrorAsync("short");

        // Act
        await FailAsync(new string('x', 1000));

        // Assert
        await Assert.That(await Page.EvalOnSelectorAsync<bool>(LogSelector, "log => log.scrollWidth > log.clientWidth")).IsTrue();
        await Assert.That(await Page.EvalOnSelectorAsync<bool>(PanelSelector, "panel => panel.scrollWidth <= panel.clientWidth")).IsTrue();
        await Assert.That(await Page.EvalOnSelectorAsync<bool>(
            LogSelector,
            "log => log.firstElementChild.offsetWidth === log.lastElementChild.offsetWidth"
            )).IsTrue();
    }

    [Test]
    public async Task BlazorError_WithLongLog_ScrollsToLatestEntry()
    {
        // Arrange
        await GoToAsync(BlazorErrorUi, $"""{Script}<link rel="stylesheet" href="/reuniverse.css" />""");

        // Act
        await Page.EvaluateAsync("""
            () => {
                for (let i = 0; i < 80; i++) {
                    console.error(`entry ${i}`);
                }
            }
            """);
        await RevealBlazorErrorAsync();

        // Assert
        await Assert.That(await Page.EvalOnSelectorAsync<bool>(
            LogSelector,
            "log => log.scrollTop > 0 && Math.abs(log.scrollTop - log.lastElementChild.offsetTop) <= 1 || log.scrollTop + log.clientHeight >= log.scrollHeight - 1"
            )).IsTrue();
    }
}
