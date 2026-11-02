using Microsoft.Playwright;

namespace Reuniverse.Razor.Tests.JavaScript;

public class UploadScreenScriptTests : BrowserTestBase
{
    private async Task OpenAsync()
    {
        await GoToWithPausedClockAsync("""<p id="target">Page</p>""");

        await Page.EvaluateAsync("""
            async () => {
                window.screen = await import("/ReuUploadScreen.razor.js");
                window.visibility = [];

                window.screen.addHandlers({
                    invokeMethodAsync: (name, visible) => window.visibility.push(visible)
                });

                window.drag = (type, withFiles = true) => {
                    // a stand-in, as browsers ignore dropEffect on a DataTransfer created by a script
                    const event = new Event(type, { bubbles: true, cancelable: true });
                    event.dataTransfer = { types: withFiles ? ["Files"] : ["text/plain"], dropEffect: "none" };

                    document.getElementById("target").dispatchEvent(event);
                    return event.dataTransfer.dropEffect;
                };
            }
            """);
    }

    private Task DragAsync(string type, bool withFiles = true)
    {
        return Page.EvaluateAsync($"""window.drag("{type}", {(withFiles ? "true" : "false")})""");
    }

    private async Task<bool[]> GetVisibilityAsync() => await Page.EvaluateAsync<bool[]>("window.visibility");

    [Test]
    public async Task DragEnter_WithFiles_ShowsScreen()
    {
        // Arrange
        await OpenAsync();

        // Act
        await DragAsync("dragenter");

        // Assert
        await (await GetVisibilityAsync()).ShouldEqualInOrderAsync(true);
    }

    [Test]
    public async Task DragEnter_WithoutFiles_DoesNothing()
    {
        // Arrange
        await OpenAsync();

        // Act
        await DragAsync("dragenter", withFiles: false);

        // Assert
        await Assert.That((await GetVisibilityAsync()).Length).IsEqualTo(0);
    }

    [Test]
    public async Task DragEnter_OverNestedElements_ShowsScreenOnce()
    {
        // Arrange
        await OpenAsync();

        // Act
        await DragAsync("dragenter");
        await DragAsync("dragenter");

        // Assert
        await (await GetVisibilityAsync()).ShouldEqualInOrderAsync(true);
    }

    [Test]
    public async Task DragLeave_FromNestedElement_KeepsScreenVisible()
    {
        // Arrange
        await OpenAsync();
        await DragAsync("dragenter");
        await DragAsync("dragenter");

        // Act
        await DragAsync("dragleave");

        // Assert
        await (await GetVisibilityAsync()).ShouldEqualInOrderAsync(true);
    }

    [Test]
    public async Task DragLeave_FromPage_HidesScreen()
    {
        // Arrange
        await OpenAsync();
        await DragAsync("dragenter");

        // Act
        await DragAsync("dragleave");

        // Assert
        await (await GetVisibilityAsync()).ShouldEqualInOrderAsync(true, false);
    }

    [Test]
    public async Task DragOver_WithFiles_ShowsCopyCursor()
    {
        // Arrange
        await OpenAsync();

        // Act
        var dropEffect = await Page.EvaluateAsync<string>("""window.drag("dragover")""");

        // Assert
        await Assert.That(dropEffect).IsEqualTo("copy");
    }

    [Test]
    public async Task Drop_WithFiles_KeepsScreenVisibleUntilBrowserIsDone()
    {
        // Arrange
        await OpenAsync();
        await DragAsync("dragenter");

        // Act
        await DragAsync("drop");

        // Assert
        await (await GetVisibilityAsync()).ShouldEqualInOrderAsync(true);
    }

    [Test]
    public async Task Drop_WithFiles_HidesScreenAfterwards()
    {
        // Arrange
        await OpenAsync();
        await DragAsync("dragenter");
        await DragAsync("drop");

        // Act
        await Page.Clock.RunForAsync(1);

        // Assert
        await (await GetVisibilityAsync()).ShouldEqualInOrderAsync(true, false);
    }

    [Test]
    public async Task DragEnter_AfterDrop_ShowsScreenAgain()
    {
        // Arrange
        await OpenAsync();
        await DragAsync("dragenter");
        await DragAsync("dragenter");
        await DragAsync("drop");
        await Page.Clock.RunForAsync(1);

        // Act
        await DragAsync("dragenter");

        // Assert
        await (await GetVisibilityAsync()).ShouldEqualInOrderAsync(true, false, true);
    }

    [Test]
    public async Task RemoveHandlers_AfterSetup_StopsReacting()
    {
        // Arrange
        await OpenAsync();
        await Page.EvaluateAsync("window.screen.removeHandlers()");

        // Act
        await DragAsync("dragenter");

        // Assert
        await Assert.That((await GetVisibilityAsync()).Length).IsEqualTo(0);
    }
}
