using Microsoft.AspNetCore.Components.Web;

namespace Reuniverse.Razor.Tests;

// the global tooltip keeps its state in statics, so tests cannot run alongside anything that touches it
[NotInParallel]
public class ReuTooltipTests : BunitContext
{
    private static readonly MouseEventArgs Cursor = new() { ClientX = 10, ClientY = 20 };

    public ReuTooltipTests()
    {
        // in this order, as HideGlobal does nothing once the text is empty
        ReuTooltip.HideGlobal();
        ReuTooltip.UpdateGlobal(null);
    }

    private IRenderedComponent<ReuTooltip> RenderGlobalTooltip()
    {
        return Render<ReuTooltip>(parameters => parameters.Add(x => x.IsGlobal, true));
    }

    [Test]
    public async Task ShowGlobal_WithText_ShowsTooltip()
    {
        // Arrange
        var cut = RenderGlobalTooltip();

        // Act
        await cut.InvokeAsync(() => ReuTooltip.ShowGlobal(Cursor, "Hint"));

        // Assert
        await Assert.That(cut.Find(".tooltip.global").TextContent.Trim()).IsEqualTo("Hint");
    }

    [Test]
    public async Task ShowGlobal_WithText_PositionsTooltipBelowCursor()
    {
        // Arrange
        var cut = RenderGlobalTooltip();

        // Act
        await cut.InvokeAsync(() => ReuTooltip.ShowGlobal(Cursor, "Hint"));

        // Assert
        await Assert.That(cut.Find(".tooltip.global").GetAttribute("style")).IsEqualTo("left:10px;top:40px;");
    }

    [Test]
    public async Task ShowGlobal_WithoutText_ShowsNothing()
    {
        // Arrange
        var cut = RenderGlobalTooltip();

        // Act
        await cut.InvokeAsync(() => ReuTooltip.ShowGlobal(Cursor, null));

        // Assert
        await Assert.That(cut.FindAll(".tooltip").Count).IsEqualTo(0);
    }

    [Test]
    public async Task MoveGlobal_WithVisibleTooltip_FollowsCursor()
    {
        // Arrange
        var cut = RenderGlobalTooltip();
        await cut.InvokeAsync(() => ReuTooltip.ShowGlobal(Cursor, "Hint"));

        // Act
        await cut.InvokeAsync(() => ReuTooltip.MoveGlobal(new MouseEventArgs { ClientX = 50, ClientY = 60 }));

        // Assert
        await Assert.That(cut.Find(".tooltip.global").GetAttribute("style")).IsEqualTo("left:50px;top:80px;");
    }

    [Test]
    public async Task MoveGlobal_WithoutText_ShowsNothing()
    {
        // Arrange
        var cut = RenderGlobalTooltip();

        // Act
        await cut.InvokeAsync(() => ReuTooltip.MoveGlobal(Cursor));

        // Assert
        await Assert.That(cut.FindAll(".tooltip").Count).IsEqualTo(0);
    }

    [Test]
    public async Task HideGlobal_WithVisibleTooltip_HidesTooltip()
    {
        // Arrange
        var cut = RenderGlobalTooltip();
        await cut.InvokeAsync(() => ReuTooltip.ShowGlobal(Cursor, "Hint"));

        // Act
        await cut.InvokeAsync(ReuTooltip.HideGlobal);

        // Assert
        await Assert.That(cut.FindAll(".tooltip").Count).IsEqualTo(0);
    }

    [Test]
    public async Task UpdateGlobal_WithVisibleTooltip_ReplacesText()
    {
        // Arrange
        var cut = RenderGlobalTooltip();
        await cut.InvokeAsync(() => ReuTooltip.ShowGlobal(Cursor, "Hint"));

        // Act
        await cut.InvokeAsync(() => ReuTooltip.UpdateGlobal("New hint"));

        // Assert
        await Assert.That(cut.Find(".tooltip.global").TextContent.Trim()).IsEqualTo("New hint");
    }

    [Test]
    public async Task Hover_OverButtonWithTooltip_ShowsGlobalTooltip()
    {
        // Arrange
        var tooltip = RenderGlobalTooltip();
        var button = Render<ReuButton>(parameters => parameters.Add(x => x.Tooltip, "Hint"));

        // Act
        await button.Find("button").TriggerEventAsync("onmouseenter", Cursor);

        // Assert
        await Assert.That(tooltip.Find(".tooltip.global").TextContent.Trim()).IsEqualTo("Hint");
    }

    [Test]
    public async Task Show_OnLocalTooltip_ShowsContent()
    {
        // Arrange
        var cut = Render<ReuTooltip>(parameters => parameters.AddChildContent("Local hint"));

        // Act
        await cut.InvokeAsync(() => cut.Instance.Show(Cursor));

        // Assert
        await Assert.That(cut.Find(".tooltip").TextContent.Trim()).IsEqualTo("Local hint");
    }

    [Test]
    public async Task Hide_OnLocalTooltip_HidesContent()
    {
        // Arrange
        var cut = Render<ReuTooltip>(parameters => parameters.AddChildContent("Local hint"));
        await cut.InvokeAsync(() => cut.Instance.Show(Cursor));

        // Act
        await cut.InvokeAsync(cut.Instance.Hide);

        // Assert
        await Assert.That(cut.FindAll(".tooltip").Count).IsEqualTo(0);
    }

    [Test]
    public async Task Show_OnGlobalTooltip_ShowsNothing()
    {
        // Arrange
        var cut = RenderGlobalTooltip();

        // Act
        await cut.InvokeAsync(() => cut.Instance.Show(Cursor));

        // Assert
        await Assert.That(cut.FindAll(".tooltip").Count).IsEqualTo(0);
    }

    [Test]
    public async Task ShowGlobal_AfterTooltipDisposed_DoesNotThrow()
    {
        // Arrange
        var cut = RenderGlobalTooltip();
        await DisposeComponentsAsync();

        // Act
        var act = () => ReuTooltip.ShowGlobal(Cursor, "Hint");

        // Assert
        await Assert.That(act).ThrowsNothing();
    }

    [Test]
    [Skip("Known bug: statics are shared between circuits, and StateHasChanged from another circuit's dispatcher throws, skipping the remaining handlers")]
    public async Task ShowGlobal_WithSecondCircuit_ShowsTooltipForThatCircuit()
    {
        // Arrange
        await using var firstCircuit = new BunitContext();
        firstCircuit.Render<ReuTooltip>(parameters => parameters.Add(x => x.IsGlobal, true));
        var cut = RenderGlobalTooltip();

        // Act
        await cut.InvokeAsync(() => ReuTooltip.ShowGlobal(Cursor, "Hint"));

        // Assert
        await Assert.That(cut.FindAll(".tooltip.global").Count).IsEqualTo(1);
    }
}
