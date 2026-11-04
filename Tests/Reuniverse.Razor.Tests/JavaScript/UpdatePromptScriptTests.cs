namespace Reuniverse.Razor.Tests.JavaScript;

public class UpdatePromptScriptTests : BrowserTestBase
{
    private const string Script = """<script src="/reuniverse.js" defer></script>""";

    private const string PromptSelector = ".reu-update-prompt";
    private const string UpdateSelector = ".reu-update-reload";
    private const string LaterSelector = ".reu-update-later";
    private const string OptOut = """data-reu-update-prompt="off" """;

    /// <summary>
    /// Stands in for the service worker API, which the insecure origin the tests run on does not have.
    /// Workers only change state when the test says so, and every load of the page is counted.
    /// </summary>
    private Task FakeServiceWorkerAsync(bool controlled = true, bool waiting = false) => Page.AddInitScriptAsync($$"""
        (() => {
            sessionStorage.setItem("loads", Number(sessionStorage.getItem("loads") ?? 0) + 1);

            class FakeWorker extends EventTarget {
                constructor(state) {
                    super();
                    this.state = state;
                    this.messages = [];
                }

                postMessage(message) {
                    this.messages.push(message);
                }

                setState(state) {
                    this.state = state;
                    this.dispatchEvent(new Event("statechange"));
                }
            }

            const registration = new EventTarget();
            registration.active = {{(controlled ? "new FakeWorker(\"activated\")" : "null")}};
            registration.installing = null;
            registration.waiting = {{(waiting ? "new FakeWorker(\"installed\")" : "null")}};
            registration.updates = 0;
            registration.update = function () {
                registration.updates++;
                return Promise.resolve(registration);
            };

            const container = new EventTarget();
            container.controller = registration.active;
            container.ready = Promise.resolve(registration);

            Object.defineProperty(navigator, "serviceWorker", { value: container, configurable: true });

            window.fakeServiceWorker = {
                registration,

                install() {
                    const worker = new FakeWorker("installing");
                    registration.installing = worker;
                    registration.dispatchEvent(new Event("updatefound"));
                    registration.installing = null;
                    registration.waiting = worker;
                    worker.setState("installed");
                },

                activate() {
                    const worker = registration.waiting;
                    registration.waiting = null;
                    registration.active = worker;
                    worker.setState("activating");
                    container.controller = worker;
                    container.dispatchEvent(new Event("controllerchange"));
                }
            };
        })();
        """);

    private Task OpenAsync() => GoToAsync("", Script);

    private Task InstallAsync() => Page.EvaluateAsync("fakeServiceWorker.install()");

    private Task ActivateAsync() => Page.EvaluateAsync("fakeServiceWorker.activate()");

    private Task<int> GetUpdateCheckCountAsync() => Page.EvaluateAsync<int>("fakeServiceWorker.registration.updates");

    private Task<int> GetLoadCountAsync() => Page.EvaluateAsync<int>("""Number(sessionStorage.getItem("loads"))""");

    private Task<string[]> GetMessageTypesAsync() => Page.EvaluateAsync<string[]>("fakeServiceWorker.registration.waiting.messages.map(message => message.type)");

    private Task<int> CountPromptsAsync() => Page.Locator(PromptSelector).CountAsync();

    private Task ShowPageAgainAsync() => Page.EvaluateAsync("""document.dispatchEvent(new Event("visibilitychange"))""");

    [Test]
    public async Task Load_WithoutServiceWorker_DoesNotShowPrompt()
    {
        // Arrange & Act
        await OpenAsync();

        // Assert
        await Assert.That(await Page.EvaluateAsync<bool>("\"serviceWorker\" in navigator")).IsFalse();
        await Assert.That(await CountPromptsAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Load_WithoutNewVersion_DoesNotShowPrompt()
    {
        // Arrange
        await FakeServiceWorkerAsync();

        // Act
        await OpenAsync();

        // Assert
        await Assert.That(await CountPromptsAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Load_WithWaitingWorker_ShowsPrompt()
    {
        // Arrange
        await FakeServiceWorkerAsync(waiting: true);

        // Act
        await OpenAsync();

        // Assert
        await Assert.That(await Page.Locator(PromptSelector).IsVisibleAsync()).IsTrue();
    }

    [Test]
    public async Task Load_WithWaitingWorkerWithoutController_DoesNotShowPrompt()
    {
        // Arrange
        // as after a hard reload, which bypasses the service worker
        await FakeServiceWorkerAsync(controlled: false, waiting: true);

        // Act
        await OpenAsync();

        // Assert
        await Assert.That(await CountPromptsAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task NewVersionInstalled_ShowsPrompt()
    {
        // Arrange
        await FakeServiceWorkerAsync();
        await OpenAsync();

        // Act
        await InstallAsync();

        // Assert
        await Assert.That(await Page.Locator(PromptSelector).IsVisibleAsync()).IsTrue();
    }

    [Test]
    public async Task FirstVersionInstalled_DoesNotShowPrompt()
    {
        // Arrange
        await FakeServiceWorkerAsync(controlled: false);
        await OpenAsync();

        // Act
        await InstallAsync();

        // Assert
        await Assert.That(await CountPromptsAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task NewVersionInstalledTwice_ShowsSinglePrompt()
    {
        // Arrange
        await FakeServiceWorkerAsync();
        await OpenAsync();

        // Act
        await InstallAsync();
        await InstallAsync();

        // Assert
        await Assert.That(await CountPromptsAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task Later_HidesPrompt()
    {
        // Arrange
        await FakeServiceWorkerAsync(waiting: true);
        await OpenAsync();

        // Act
        await Page.Locator(LaterSelector).ClickAsync();

        // Assert
        await Assert.That(await CountPromptsAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Later_ThenNewerVersion_ShowsPromptAgain()
    {
        // Arrange
        await FakeServiceWorkerAsync(waiting: true);
        await OpenAsync();
        await Page.Locator(LaterSelector).ClickAsync();

        // Act
        await InstallAsync();

        // Assert
        await Assert.That(await Page.Locator(PromptSelector).IsVisibleAsync()).IsTrue();
    }

    [Test]
    public async Task Update_AsksWaitingWorkerToTakeOver()
    {
        // Arrange
        await FakeServiceWorkerAsync(waiting: true);
        await OpenAsync();

        // Act
        await Page.Locator(UpdateSelector).ClickAsync();

        // Assert
        await Assert.That(await GetMessageTypesAsync()).IsEquivalentTo(["SKIP_WAITING"]);
        await Assert.That(await Page.Locator(UpdateSelector).IsDisabledAsync()).IsTrue();
        await Assert.That(await GetLoadCountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task Update_ThenNewWorkerTakesOver_Reloads()
    {
        // Arrange
        await FakeServiceWorkerAsync(waiting: true);
        await OpenAsync();
        await Page.Locator(UpdateSelector).ClickAsync();

        // Act
        await RunAndWaitForReloadAsync(ActivateAsync);

        // Assert
        await Assert.That(await GetLoadCountAsync()).IsEqualTo(2);
    }

    [Test]
    public async Task Update_AfterWorkerTookOverOnItsOwn_Reloads()
    {
        // Arrange
        // as with a service worker that calls skipWaiting() on install
        await FakeServiceWorkerAsync();
        await OpenAsync();
        await InstallAsync();
        await ActivateAsync();

        // Act
        await RunAndWaitForReloadAsync(() => Page.Locator(UpdateSelector).ClickAsync());

        // Assert
        await Assert.That(await GetLoadCountAsync()).IsEqualTo(2);
    }

    [Test]
    public async Task NewWorkerTakesOver_WithoutUpdate_DoesNotReload()
    {
        // Arrange
        // as when the update was accepted in another tab
        await FakeServiceWorkerAsync(waiting: true);
        await OpenAsync();

        // Act
        await ActivateAsync();
        await Page.WaitForTimeoutAsync(500);

        // Assert
        await Assert.That(await GetLoadCountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task ReplaceBody_LikeEnhancedNavigation_KeepsPromptOpen()
    {
        // Arrange
        await FakeServiceWorkerAsync(waiting: true);
        await OpenAsync();

        // Act
        await Page.EvaluateAsync("document.body.replaceChildren()");
        await Page.Locator(PromptSelector).WaitForAsync();

        // Assert
        await Assert.That(await CountPromptsAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task HourPassed_ChecksForUpdate()
    {
        // Arrange
        await FakeServiceWorkerAsync();
        await GoToWithPausedClockAsync("", Script);

        // Act
        await Page.Clock.RunForAsync(60 * 60 * 1000);

        // Assert
        await Assert.That(await GetUpdateCheckCountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task PageShownAgain_SoonAfterLastCheck_DoesNotCheckForUpdate()
    {
        // Arrange
        await FakeServiceWorkerAsync();
        await GoToWithPausedClockAsync("", Script);

        // Act
        await Page.Clock.RunForAsync(60 * 1000);
        await ShowPageAgainAsync();

        // Assert
        await Assert.That(await GetUpdateCheckCountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task PageShownAgain_LongAfterLastCheck_ChecksForUpdate()
    {
        // Arrange
        await FakeServiceWorkerAsync();
        await GoToWithPausedClockAsync("", Script);

        // Act
        await Page.Clock.RunForAsync(5 * 60 * 1000);
        await ShowPageAgainAsync();

        // Assert
        await Assert.That(await GetUpdateCheckCountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task UpdateCheckFails_DoesNotReportError()
    {
        // Arrange
        await FakeServiceWorkerAsync();
        await Page.AddInitScriptAsync("""fakeServiceWorker.registration.update = () => Promise.reject(new TypeError("Failed to fetch"))""");
        var errors = new List<string>();
        Page.PageError += (_, error) => errors.Add(error);
        await GoToWithPausedClockAsync("", Script);

        // Act
        await Page.Clock.RunForAsync(60 * 60 * 1000);

        // Assert
        await Assert.That(errors).IsEmpty();
    }

    [Test]
    public async Task Load_WithWaitingWorker_OptedOut_DoesNotShowPrompt()
    {
        // Arrange
        await FakeServiceWorkerAsync(waiting: true);

        // Act
        await GoToAsync("", Script, OptOut);

        // Assert
        await Assert.That(await CountPromptsAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task NewVersionInstalled_OptedOut_DoesNotShowPrompt()
    {
        // Arrange
        await FakeServiceWorkerAsync();
        await GoToAsync("", Script, OptOut);

        // Act
        await InstallAsync();

        // Assert
        await Assert.That(await CountPromptsAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task HourPassed_OptedOut_DoesNotCheckForUpdate()
    {
        // Arrange
        await FakeServiceWorkerAsync();
        await GoToWithPausedClockAsync("", Script, OptOut);

        // Act
        await Page.Clock.RunForAsync(60 * 60 * 1000);

        // Assert
        await Assert.That(await GetUpdateCheckCountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task PageShownAgain_OptedOut_DoesNotCheckForUpdate()
    {
        // Arrange
        await FakeServiceWorkerAsync();
        await GoToWithPausedClockAsync("", Script, OptOut);

        // Act
        await Page.Clock.RunForAsync(5 * 60 * 1000);
        await ShowPageAgainAsync();

        // Assert
        await Assert.That(await GetUpdateCheckCountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task Load_WithWaitingWorker_OtherOptOutValue_ShowsPrompt()
    {
        // Arrange
        await FakeServiceWorkerAsync(waiting: true);

        // Act
        await GoToAsync("", Script, """data-reu-update-prompt="on" """);

        // Assert
        await Assert.That(await Page.Locator(PromptSelector).IsVisibleAsync()).IsTrue();
    }
}
