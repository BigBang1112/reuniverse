using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Reuniverse.Razor.Tests;

public class ReuDialogServiceTests : BunitContext
{
    private readonly ReuDialogService dialogs = new();

    public ReuDialogServiceTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    private IRenderedComponent<ReuDialogHost> RenderHost(ReuDialogService? service = null)
    {
        return Render<ReuDialogHost>(parameters => parameters.Add(x => x.Service, service ?? dialogs));
    }

    // asks on the renderer's dispatcher, and returns the answer still to come
    private async Task<Task<bool>> AskAsync(IRenderedComponent<ReuDialogHost> host, string message, ReuConfirmOptions? options = null, CancellationToken cancellationToken = default)
    {
        Task<bool>? answer = null;
        // a statement, as InvokeAsync would otherwise wait for the returned task, which is the answer
        await host.InvokeAsync(() => { answer = dialogs.ConfirmAsync(message, options, cancellationToken); });
        return answer!;
    }

    [Test]
    public async Task ConfirmAsync_WithoutHost_Throws()
    {
        // Act
        var act = () => dialogs.ConfirmAsync("Discard changes?");

        // Assert
        await Assert.That(act).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ConfirmAsync_WithHost_ShowsQuestionInModal()
    {
        // Arrange
        var host = RenderHost();

        // Act
        _ = await AskAsync(host, "Discard changes?", new ReuConfirmOptions { Title = "Unsaved changes", ConfirmText = "Discard", CancelText = "Keep editing" });

        // Assert
        var panel = host.Find(".reu-modal-panel");
        await Assert.That(panel.GetAttribute("role")).IsEqualTo("alertdialog");
        await Assert.That(host.Find(".reu-modal-title").TextContent.Trim()).IsEqualTo("Unsaved changes");
        var message = host.Find(".reu-confirm-message");
        await Assert.That(message.TextContent).IsEqualTo("Discard changes?");
        await Assert.That(panel.GetAttribute("aria-describedby")).IsEqualTo(message.Id);
        await Assert.That(host.Find(".reu-confirm-ok").TextContent.Trim()).IsEqualTo("Discard");
        await Assert.That(host.Find(".reu-confirm-cancel").TextContent.Trim()).IsEqualTo("Keep editing");
    }

    [Test]
    public async Task ConfirmAsync_WithoutOptions_UsesDefaults()
    {
        // Arrange
        var host = RenderHost();

        // Act
        _ = await AskAsync(host, "Continue?");

        // Assert
        await Assert.That(host.Find(".reu-modal-title").TextContent.Trim()).IsEqualTo("Confirm");
        await Assert.That(host.Find(".reu-confirm-ok").TextContent.Trim()).IsEqualTo("OK");
        await Assert.That(host.Find(".reu-confirm-cancel").TextContent.Trim()).IsEqualTo("Cancel");
    }

    [Test]
    public async Task ClickOk_WhenAsked_Confirms()
    {
        // Arrange
        var host = RenderHost();
        var answer = await AskAsync(host, "Continue?");

        // Act
        host.Find(".reu-confirm-ok").Click();

        // Assert
        await Assert.That(await answer).IsTrue();
        await Assert.That(host.FindAll(".reu-modal-panel").Count).IsEqualTo(0);
    }

    [Test]
    public async Task ClickCancel_WhenAsked_Declines()
    {
        // Arrange
        var host = RenderHost();
        var answer = await AskAsync(host, "Continue?");

        // Act
        host.Find(".reu-confirm-cancel").Click();

        // Assert
        await Assert.That(await answer).IsFalse();
    }

    [Test]
    public async Task CloseButton_WhenAsked_Declines()
    {
        // Arrange
        var host = RenderHost();
        var answer = await AskAsync(host, "Continue?");

        // Act
        host.Find(".reu-modal-close-btn").Click();

        // Assert
        await Assert.That(await answer).IsFalse();
    }

    [Test]
    public async Task Escape_WhenAsked_Declines()
    {
        // Arrange
        var host = RenderHost();
        var answer = await AskAsync(host, "Continue?");

        // Act
        host.Find(".reu-modal-panel").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        // Assert
        await Assert.That(await answer).IsFalse();
    }

    [Test]
    public async Task ConfirmAsync_NotDestructive_FocusesConfirmButton()
    {
        // Arrange
        var host = RenderHost();

        // Act
        _ = await AskAsync(host, "Continue?");

        // Assert
        await Assert.That(host.Find(".reu-confirm-ok").HasAttribute("autofocus")).IsTrue();
        await Assert.That(host.Find(".reu-confirm-ok").ClassList.Contains("reu-button-blue")).IsTrue();
        await Assert.That(host.Find(".reu-confirm-cancel").HasAttribute("autofocus")).IsFalse();
    }

    [Test]
    public async Task ConfirmAsync_Destructive_MakesConfirmRedAndFocusesCancel()
    {
        // Arrange
        var host = RenderHost();

        // Act
        _ = await AskAsync(host, "Remove #1?", new ReuConfirmOptions { Destructive = true });

        // Assert
        await Assert.That(host.Find(".reu-confirm-ok").ClassList.Contains("reu-button-red")).IsTrue();
        await Assert.That(host.Find(".reu-confirm-cancel").HasAttribute("autofocus")).IsTrue();
        await Assert.That(host.Find(".reu-confirm-ok").HasAttribute("autofocus")).IsFalse();
    }

    [Test]
    public async Task ConfirmAsync_WhileAnotherIsOpen_WaitsForIt()
    {
        // Arrange
        var host = RenderHost();
        var first = await AskAsync(host, "First?");
        var second = await AskAsync(host, "Second?");
        var shownFirst = host.Find(".reu-confirm-message").TextContent;

        // Act
        host.Find(".reu-confirm-ok").Click();

        // Assert
        await Assert.That(shownFirst).IsEqualTo("First?");
        await Assert.That(await first).IsTrue();
        await Assert.That(second.IsCompleted).IsFalse();
        host.WaitForState(() => host.FindAll(".reu-confirm-message").FirstOrDefault()?.TextContent == "Second?", TimeSpan.FromSeconds(10));
    }

    [Test]
    public async Task ConfirmAsync_WhenCancelled_TakesQuestionBack()
    {
        // Arrange
        var host = RenderHost();
        using var cancellation = new CancellationTokenSource();
        var answer = await AskAsync(host, "Continue?", cancellationToken: cancellation.Token);

        // Act
        await host.InvokeAsync(cancellation.Cancel);

        // Assert
        await Assert.That(() => answer).Throws<TaskCanceledException>();
        await Assert.That(dialogs.Current).IsNull();
        host.WaitForState(() => host.FindAll(".reu-modal-panel").Count == 0, TimeSpan.FromSeconds(10));
    }

    [Test]
    public async Task Respond_Twice_KeepsFirstAnswer()
    {
        // Arrange
        var host = RenderHost();
        var answer = await AskAsync(host, "Continue?");
        var request = dialogs.Current!;

        // Act
        await host.InvokeAsync(() => request.Respond(false));
        await host.InvokeAsync(() => request.Respond(true));

        // Assert
        await Assert.That(await answer).IsFalse();
    }

    [Test]
    public async Task DisposeHost_WithQuestionOpen_DeclinesIt()
    {
        // Arrange
        var host = RenderHost();
        var answer = await AskAsync(host, "Continue?");

        // Act
        await DisposeComponentsAsync();

        // Assert
        await Assert.That(await answer).IsFalse();
        await Assert.That(dialogs.HasHost).IsFalse();
    }

    [Test]
    public async Task Render_WithoutServiceParameter_UsesRegisteredOne()
    {
        // Arrange
        // a context of its own, as this one already handed out services, after which nothing can be registered
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddReuDialogs();
        context.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        var registered = context.Services.GetRequiredService<ReuDialogService>();

        // Act
        var host = context.Render<ReuDialogHost>();

        // Assert
        await Assert.That(registered.HasHost).IsTrue();
        _ = await host.InvokeAsync(() => { _ = registered.ConfirmAsync("Registered?"); return true; });
        await Assert.That(host.Find(".reu-confirm-message").TextContent).IsEqualTo("Registered?");
    }

    [Test]
    public async Task Render_WithoutAnyService_Throws()
    {
        // Act
        var act = () => Render<ReuDialogHost>();

        // Assert
        await Assert.That(act).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ObjectTree_WithoutDialogs_AsksWithItsOwnDialog()
    {
        // Arrange
        var list = new List<int> { 1, 2 };
        var cut = Render<ReuObjectTree>(parameters => parameters.Add(x => x.Value, list).Add(x => x.IsExpandedByDefault, true));

        // Act
        // the click waits for the answer, so it is not awaited
        _ = cut.FindAll(".remove")[1].ClickAsync(new MouseEventArgs());

        // Assert
        await Assert.That(cut.Find(".reu-confirm-message").TextContent).IsEqualTo("Remove #1?");
        await Assert.That(cut.Find(".reu-confirm-ok").TextContent.Trim()).IsEqualTo("Remove");
        await list.ShouldEqualInOrderAsync(1, 2);
    }

    [Test]
    public async Task ObjectTree_ConfirmedInItsOwnDialog_RemovesElement()
    {
        // Arrange
        var list = new List<int> { 1, 2 };
        var cut = Render<ReuObjectTree>(parameters => parameters.Add(x => x.Value, list).Add(x => x.IsExpandedByDefault, true));
        var removing = cut.FindAll(".remove")[1].ClickAsync(new MouseEventArgs());

        // Act
        cut.Find(".reu-confirm-ok").Click();
        await removing;

        // Assert
        await list.ShouldEqualInOrderAsync(1);
    }

    [Test]
    public async Task ObjectTree_DeclinedInItsOwnDialog_KeepsElement()
    {
        // Arrange
        var list = new List<int> { 1, 2 };
        var cut = Render<ReuObjectTree>(parameters => parameters.Add(x => x.Value, list).Add(x => x.IsExpandedByDefault, true));
        var removing = cut.FindAll(".remove")[1].ClickAsync(new MouseEventArgs());

        // Act
        cut.Find(".reu-confirm-cancel").Click();
        await removing;

        // Assert
        await list.ShouldEqualInOrderAsync(1, 2);
        await Assert.That(cut.FindAll(".reu-confirm-message").Count).IsEqualTo(0);
    }

    [Test]
    public async Task ObjectTree_WithSharedDialogs_AsksThroughThemWithoutOwnHost()
    {
        // Arrange
        var host = RenderHost();
        var list = new List<int> { 1, 2 };
        var cut = Render<ReuObjectTree>(parameters => parameters
            .Add(x => x.Value, list)
            .Add(x => x.IsExpandedByDefault, true)
            .Add(x => x.Dialogs, dialogs));

        // Act
        _ = cut.FindAll(".remove")[0].ClickAsync(new MouseEventArgs());

        // Assert
        await Assert.That(cut.FindAll(".reu-confirm-message").Count).IsEqualTo(0);
        await Assert.That(host.Find(".reu-confirm-message").TextContent).IsEqualTo("Remove #0?");
    }
}
