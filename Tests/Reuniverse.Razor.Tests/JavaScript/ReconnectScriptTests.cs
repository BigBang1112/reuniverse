namespace Reuniverse.Razor.Tests.JavaScript;

public class ReconnectScriptTests : BrowserTestBase
{
    private const string Head = """
        <title>Reconnect</title>
        <link rel="stylesheet" href="/reuniverse.css" />
        <link rel="stylesheet" href="/Reuniverse.Razor.styles.css" />
        <script src="/reuniverse.js" defer></script>
        """;

    // does to the dialog what Blazor does, and stands in for the Blazor functions the script calls
    private const string Emulator = """
        <button id="behind">Behind</button>
        <script>
            window.calls = [];
            window.reconnectResult = true;
            window.resumeResult = true;
            window.resumeThrows = false;
            window.Blazor = {
                reconnect: async () => { calls.push("reconnect"); return window.reconnectResult; },
                resumeCircuit: async () => { calls.push("resume"); if (window.resumeThrows) throw new Error("failed"); return window.resumeResult; }
            };

            const names = ["show", "hide", "retrying", "failed", "rejected", "paused", "resume-failed"].map(x => "components-reconnect-" + x);

            window.blazorState = (state, currentAttempt, secondsToNextAttempt) => {
                const dialog = document.getElementById("components-reconnect-modal");
                const detail = { state };

                if (state === "retrying") {
                    document.getElementById("components-reconnect-current-attempt").innerText = currentAttempt;
                    document.getElementById("components-seconds-to-next-attempt").innerText = secondsToNextAttempt;
                    if (currentAttempt > 1 && secondsToNextAttempt > 0) {
                        dialog.classList.add("components-reconnect-retrying");
                    }
                    detail.currentAttempt = currentAttempt;
                    detail.secondsToNextAttempt = secondsToNextAttempt;
                } else if (state === "paused") {
                    dialog.classList.remove("components-reconnect-show", "components-reconnect-retrying");
                    dialog.classList.add("components-reconnect-paused");
                } else {
                    dialog.classList.remove(...names);
                    dialog.classList.add("components-reconnect-" + state);
                }

                dialog.dispatchEvent(new CustomEvent("components-reconnect-state-changed", { detail }));
            };
        </script>
        """;

    private static string Markup(int? delay = null, bool reloadOnRejected = true)
    {
        using var context = new BunitContext();
        return context.Render<ReuReconnectModal>(parameters =>
        {
            if (delay is int value)
            {
                parameters.Add(x => x.Delay, value);
            }

            parameters.Add(x => x.ReloadOnRejected, reloadOnRejected);
        }).Markup;
    }

    private async Task OpenAsync(int? delay = null, bool reloadOnRejected = true)
    {
        await GoToWithPausedClockAsync(Markup(delay, reloadOnRejected) + Emulator, Head, """lang="en" """);
        await Page.WaitForFunctionAsync("() => document.readyState === 'complete'");
    }

    private Task StateAsync(string state, int currentAttempt = 0, int secondsToNextAttempt = 0)
    {
        return Page.EvaluateAsync("([state, attempt, seconds]) => blazorState(state, attempt, seconds)", new object[] { state, currentAttempt, secondsToNextAttempt });
    }

    private Task<bool> IsDialogOpenAsync() => Page.EvaluateAsync<bool>("() => document.getElementById('components-reconnect-modal').open");

    private Task<bool> HasClassAsync(string selector, string name) => Page.EvaluateAsync<bool>("([s, n]) => document.querySelector(s).classList.contains(n)", new[] { selector, name });

    private Task<bool> IsShownAsync(string selector) => Page.EvaluateAsync<bool>("s => getComputedStyle(document.querySelector(s)).display !== 'none'", selector);

    private Task<string[]> GetCallsAsync() => Page.EvaluateAsync<string[]>("() => window.calls");

    // lets the promises of the stand-in functions settle
    private Task SettleAsync() => Page.EvaluateAsync("() => new Promise(resolve => queueMicrotask(resolve))");

    [Test]
    public async Task Show_BriefDrop_ShowsNoticeAndHoldsClicksBack()
    {
        // Arrange
        await OpenAsync();

        // Act
        await StateAsync("show");

        // Assert
        await Assert.That(await IsDialogOpenAsync()).IsFalse();
        await Assert.That(await HasClassAsync(".reu-reconnect-toast", "visible")).IsTrue();
        await Assert.That(await HasClassAsync(".reu-reconnect-shield", "visible")).IsTrue();
        await Assert.That(await Page.Locator(".reu-reconnect-toast").TextContentAsync()).IsEqualTo("Reconnecting…");
    }

    [Test]
    public async Task Show_LongerThanDelay_OpensDialog()
    {
        // Arrange
        await OpenAsync();
        await StateAsync("show");

        // Act
        await Page.Clock.RunForAsync(1500);

        // Assert
        await Assert.That(await IsDialogOpenAsync()).IsTrue();
        await Assert.That(await HasClassAsync(".reu-reconnect-toast", "visible")).IsFalse();
        await Assert.That(await HasClassAsync(".reu-reconnect-shield", "visible")).IsFalse();
        await Assert.That(await IsShownAsync(".reu-reconnect-title .when-connecting")).IsTrue();
        await Assert.That(await IsShownAsync(".reu-reconnect-spinner")).IsTrue();
    }

    [Test]
    public async Task Hide_WithinDelay_NeverOpensDialogAndSaysConnected()
    {
        // Arrange
        await OpenAsync();
        await StateAsync("show");
        await Page.Clock.RunForAsync(500);

        // Act
        await StateAsync("hide");
        await Page.Clock.RunForAsync(1500);

        // Assert
        await Assert.That(await IsDialogOpenAsync()).IsFalse();
        await Assert.That(await HasClassAsync(".reu-reconnect-shield", "visible")).IsFalse();
        await Assert.That(await HasClassAsync(".reu-reconnect-toast", "connected")).IsTrue();
        await Assert.That(await Page.Locator(".reu-reconnect-toast").TextContentAsync()).IsEqualTo("Connected again");
    }

    [Test]
    public async Task Hide_AfterAWhile_TakesConnectedNoticeAway()
    {
        // Arrange
        await OpenAsync();
        await StateAsync("show");
        await StateAsync("hide");

        // Act
        await Page.Clock.RunForAsync(2000);

        // Assert
        await Assert.That(await HasClassAsync(".reu-reconnect-toast", "visible")).IsFalse();
    }

    [Test]
    public async Task Hide_WithDialogOpen_ClosesIt()
    {
        // Arrange
        await OpenAsync(delay: 0);
        await StateAsync("show");

        // Act
        await StateAsync("hide");

        // Assert
        await Assert.That(await IsDialogOpenAsync()).IsFalse();
    }

    [Test]
    public async Task Show_WithoutDelay_OpensDialogRightAway()
    {
        // Arrange
        await OpenAsync(delay: 0);

        // Act
        await StateAsync("show");

        // Assert
        await Assert.That(await IsDialogOpenAsync()).IsTrue();
        await Assert.That(await HasClassAsync(".reu-reconnect-toast", "visible")).IsFalse();
    }

    [Test]
    public async Task Retrying_FirstAttempt_StaysQuiet()
    {
        // Arrange
        await OpenAsync();
        await StateAsync("show");

        // Act
        await StateAsync("retrying", 1, 0);

        // Assert
        await Assert.That(await IsDialogOpenAsync()).IsFalse();
    }

    [Test]
    public async Task Retrying_QuickFailedAttempts_StayQuietUntilDelay()
    {
        // Arrange
        await OpenAsync();
        await StateAsync("show");

        // Act
        // Blazor tries again right away a few times when the server cannot be reached at all
        await StateAsync("retrying", 2, 0);
        await StateAsync("retrying", 3, 0);
        var openEarly = await IsDialogOpenAsync();
        await Page.Clock.RunForAsync(1500);

        // Assert
        await Assert.That(openEarly).IsFalse();
        await Assert.That(await IsDialogOpenAsync()).IsTrue();
    }

    [Test]
    public async Task Retrying_WithWait_ShowsCountdown()
    {
        // Arrange
        await OpenAsync();
        await StateAsync("show");

        // Act
        await StateAsync("retrying", 11, 5);
        await Page.Clock.RunForAsync(1500);

        // Assert
        await Assert.That(await IsDialogOpenAsync()).IsTrue();
        await Assert.That(await IsShownAsync(".reu-reconnect-progress")).IsTrue();
        await Assert.That(await IsShownAsync(".reu-reconnect-text .when-retrying")).IsTrue();
        await Assert.That(await GetWaitAsync()).IsEqualTo("5s");
        await Assert.That(await Page.Locator("#components-reconnect-current-attempt").TextContentAsync()).IsEqualTo("11");
    }

    [Test]
    public async Task Retrying_SameAttemptCountingDown_KeepsCountdownGoing()
    {
        // Arrange
        await OpenAsync();
        await StateAsync("show");
        await StateAsync("retrying", 11, 5);

        // Act
        // Blazor tells the seconds left every second
        await StateAsync("retrying", 11, 4);
        var sameAttempt = await GetWaitAsync();
        await StateAsync("retrying", 12, 8);

        // Assert
        await Assert.That(sameAttempt).IsEqualTo("5s");
        await Assert.That(await GetWaitAsync()).IsEqualTo("8s");
    }

    private Task<string> GetWaitAsync() => Page.EvaluateAsync<string>("() => document.querySelector('.reu-reconnect-progress').style.getPropertyValue('--reu-reconnect-wait')");

    [Test]
    public async Task Failed_ShowsRetryAndReload()
    {
        // Arrange
        await OpenAsync();
        await StateAsync("show");

        // Act
        await StateAsync("failed");

        // Assert
        await Assert.That(await IsDialogOpenAsync()).IsTrue();
        await Assert.That(await IsShownAsync(".reu-reconnect-alert")).IsTrue();
        await Assert.That(await Page.Locator("[data-reu-reconnect=retry]").IsVisibleAsync()).IsTrue();
        await Assert.That(await Page.Locator("[data-reu-reconnect=reload].when-failed").IsVisibleAsync()).IsTrue();
        await Assert.That(await Page.Locator("[data-reu-reconnect=resume]").IsVisibleAsync()).IsFalse();
    }

    [Test]
    public async Task ClickRetry_WhenFailed_Reconnects()
    {
        // Arrange
        await OpenAsync();
        await StateAsync("failed");

        // Act
        await Page.Locator("[data-reu-reconnect=retry]").ClickAsync();
        await SettleAsync();

        // Assert
        await (await GetCallsAsync()).ShouldEqualInOrderAsync("reconnect");
    }

    [Test]
    public async Task ClickRetry_WithCircuitGoneFromServer_ResumesIt()
    {
        // Arrange
        await OpenAsync();
        await StateAsync("failed");
        await Page.EvaluateAsync("() => window.reconnectResult = false");

        // Act
        await Page.Locator("[data-reu-reconnect=retry]").ClickAsync();
        await Page.WaitForFunctionAsync("() => window.calls.length === 2");

        // Assert
        await (await GetCallsAsync()).ShouldEqualInOrderAsync("reconnect", "resume");
    }

    [Test]
    public async Task ClickRetry_WithNothingToResume_Reloads()
    {
        // Arrange
        await OpenAsync();
        await StateAsync("failed");
        await Page.EvaluateAsync("() => { window.reconnectResult = false; window.resumeResult = false; }");

        // Act & Assert
        await RunAndWaitForReloadAsync(() => Page.Locator("[data-reu-reconnect=retry]").ClickAsync());
    }

    [Test]
    public async Task BackOnline_WhenFailed_RetriesOnItsOwn()
    {
        // Arrange
        await OpenAsync();
        await Page.Context.SetOfflineAsync(true);
        await StateAsync("failed");
        var offline = await HasClassAsync("#components-reconnect-modal", "offline");

        // Act
        await Page.Context.SetOfflineAsync(false);
        await Page.WaitForFunctionAsync("() => window.calls.length > 0");

        // Assert
        await Assert.That(offline).IsTrue();
        await Assert.That(await IsShownAsync(".reu-reconnect-text .when-offline")).IsFalse();
        await (await GetCallsAsync()).ShouldEqualInOrderAsync("reconnect");
    }

    [Test]
    public async Task Show_WhileOffline_SaysSo()
    {
        // Arrange
        await OpenAsync();
        await Page.Context.SetOfflineAsync(true);

        // Act
        await StateAsync("show");
        await Page.Clock.RunForAsync(1500);

        // Assert
        await Assert.That(await IsShownAsync(".reu-reconnect-text .when-offline")).IsTrue();
        await Assert.That(await IsShownAsync(".reu-reconnect-text .when-online")).IsFalse();
        await Page.Context.SetOfflineAsync(false);
    }

    [Test]
    public async Task Show_AfterFailedWithDialogOpen_KeepsDialog()
    {
        // Arrange
        await OpenAsync();
        await StateAsync("failed");

        // Act
        await StateAsync("show");

        // Assert
        await Assert.That(await IsDialogOpenAsync()).IsTrue();
        await Assert.That(await HasClassAsync(".reu-reconnect-toast", "visible")).IsFalse();
    }

    [Test]
    public async Task Paused_ShowsResume()
    {
        // Arrange
        await OpenAsync();

        // Act
        await StateAsync("paused");

        // Assert
        await Assert.That(await IsDialogOpenAsync()).IsTrue();
        await Assert.That(await IsShownAsync(".reu-reconnect-pause")).IsTrue();
        await Assert.That(await Page.Locator("[data-reu-reconnect=resume]").IsVisibleAsync()).IsTrue();
        await Assert.That(await Page.Locator("[data-reu-reconnect=retry]").IsVisibleAsync()).IsFalse();
    }

    [Test]
    public async Task ClickResume_WhenPaused_ResumesCircuit()
    {
        // Arrange
        await OpenAsync();
        await StateAsync("paused");

        // Act
        await Page.Locator("[data-reu-reconnect=resume]").ClickAsync();
        await SettleAsync();

        // Assert
        await (await GetCallsAsync()).ShouldEqualInOrderAsync("resume");
    }

    [Test]
    public async Task ClickResume_WhenItThrows_SaysResumeFailed()
    {
        // Arrange
        await OpenAsync();
        await StateAsync("paused");
        await Page.EvaluateAsync("() => window.resumeThrows = true");

        // Act
        await Page.Locator("[data-reu-reconnect=resume]").ClickAsync();
        await Page.WaitForFunctionAsync("() => document.getElementById('components-reconnect-modal').classList.contains('components-reconnect-resume-failed')");

        // Assert
        await Assert.That(await IsShownAsync(".reu-reconnect-title .when-resume-failed")).IsTrue();
        await Assert.That(await Page.Locator("[data-reu-reconnect=reload].when-resume-failed").IsVisibleAsync()).IsTrue();
    }

    [Test]
    public async Task Rejected_SaysSoThenReloads()
    {
        // Arrange
        await OpenAsync();

        // Act & Assert
        await RunAndWaitForReloadAsync(async () =>
        {
            await StateAsync("rejected");
            await Assert.That(await IsShownAsync(".reu-reconnect-title .when-rejected")).IsTrue();
            await Page.Clock.RunForAsync(1500);
        });
    }

    [Test]
    public async Task Rejected_WithReloadOff_OffersReloadInstead()
    {
        // Arrange
        await OpenAsync(reloadOnRejected: false);
        await Page.EvaluateAsync("() => window.stillHere = true");

        // Act
        await StateAsync("rejected");
        await Page.Clock.RunForAsync(5000);

        // Assert
        await Assert.That(await Page.EvaluateAsync<bool>("() => window.stillHere === true")).IsTrue();
        await Assert.That(await Page.Locator("[data-reu-reconnect=reload].when-rejected-manual").IsVisibleAsync()).IsTrue();
    }

    [Test]
    public async Task Escape_WithDialogOpen_KeepsIt()
    {
        // Arrange
        await OpenAsync();
        await StateAsync("failed");

        // Act
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await Assert.That(await IsDialogOpenAsync()).IsTrue();
    }

    [Test]
    public async Task OpenDialog_MakesPageBehindUnreachable()
    {
        // Arrange
        await OpenAsync();

        // Act
        await StateAsync("failed");
        await Page.EvaluateAsync("() => document.getElementById('behind').focus()");

        // Assert
        await Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement?.id ?? ''")).IsNotEqualTo("behind");
    }

    [Test]
    public async Task StateChange_OnOtherDialog_IsLeftAlone()
    {
        // Arrange
        await GoToWithPausedClockAsync("""<dialog id="components-reconnect-modal"></dialog><div class="reu-reconnect-toast"></div>""", Head);

        // Act
        await Page.EvaluateAsync("""
            () => document.getElementById("components-reconnect-modal").dispatchEvent(new CustomEvent("components-reconnect-state-changed", { detail: { state: "show" } }))
            """);
        await Page.Clock.RunForAsync(5000);

        // Assert
        await Assert.That(await IsDialogOpenAsync()).IsFalse();
        await Assert.That(await HasClassAsync(".reu-reconnect-toast", "visible")).IsFalse();
    }

    private Task<string?> FocusedActionAsync() => Page.EvaluateAsync<string?>("() => document.activeElement?.getAttribute('data-reu-reconnect') ?? document.activeElement?.tagName");

    [Test]
    public async Task Failed_AfterConnectingDialog_FocusesRetry()
    {
        // Arrange
        await OpenAsync(delay: 0);
        await StateAsync("show");
        var first = await FocusedActionAsync();

        // Act
        await StateAsync("failed");

        // Assert
        await Assert.That(first).IsEqualTo("DIALOG");
        await Assert.That(await FocusedActionAsync()).IsEqualTo("retry");
    }

    [Test]
    public async Task Paused_FocusesResume()
    {
        // Arrange
        await OpenAsync();

        // Act
        await StateAsync("paused");

        // Assert
        await Assert.That(await FocusedActionAsync()).IsEqualTo("resume");
    }
}
