namespace Reuniverse.Razor.Tests;

public class ReuReconnectModalTests : BunitContext
{
    [Test]
    public async Task Render_Default_GivesBlazorTheDialogItLooksFor()
    {
        // Act
        var cut = Render<ReuReconnectModal>();

        // Assert
        var dialog = cut.Find("dialog#components-reconnect-modal");
        await Assert.That(dialog.ClassList.Contains("reu-reconnect")).IsTrue();
        await Assert.That(dialog.GetAttribute("data-reu-reconnect-delay")).IsEqualTo("1500");
        await Assert.That(dialog.GetAttribute("data-reu-reconnect-reload")).IsEqualTo("true");
        await Assert.That(cut.FindAll("#components-reconnect-current-attempt").Count).IsEqualTo(1);
        await Assert.That(cut.FindAll("#components-reconnect-max-retries").Count).IsEqualTo(1);
        await Assert.That(cut.FindAll("#components-seconds-to-next-attempt").Count).IsEqualTo(1);
    }

    [Test]
    public async Task Render_Default_SurvivesEnhancedNavigation()
    {
        // Act
        var cut = Render<ReuReconnectModal>();

        // Assert
        await Assert.That(cut.Find(".reu-reconnect-root").HasAttribute("data-permanent")).IsTrue();
        await Assert.That(cut.Find(".reu-reconnect-root").Id).IsNotNull();
    }

    [Test]
    public async Task Render_Default_NamesDialogAndAnnouncesNotices()
    {
        // Act
        var cut = Render<ReuReconnectModal>();

        // Assert
        var dialog = cut.Find("dialog");
        await Assert.That(cut.Find($"#{dialog.GetAttribute("aria-labelledby")}").ClassList.Contains("reu-reconnect-title")).IsTrue();
        await Assert.That(cut.Find($"#{dialog.GetAttribute("aria-describedby")}").ClassList.Contains("reu-reconnect-text")).IsTrue();
        await Assert.That(cut.Find(".reu-reconnect-toast").GetAttribute("role")).IsEqualTo("status");
    }

    [Test]
    public async Task Render_Default_HasButtonsForEachAction()
    {
        // Act
        var cut = Render<ReuReconnectModal>();

        // Assert
        var actions = cut.FindAll("[data-reu-reconnect]").Select(x => x.GetAttribute("data-reu-reconnect")).Distinct().Order();
        await actions.ShouldEqualInOrderAsync("reload", "resume", "retry");
    }

    [Test]
    public async Task Render_WithParameters_PassesThemToScript()
    {
        // Act
        var cut = Render<ReuReconnectModal>(parameters => parameters
            .Add(x => x.Delay, 0)
            .Add(x => x.ReloadOnRejected, false));

        // Assert
        var dialog = cut.Find("dialog");
        await Assert.That(dialog.GetAttribute("data-reu-reconnect-delay")).IsEqualTo("0");
        await Assert.That(dialog.GetAttribute("data-reu-reconnect-reload")).IsEqualTo("false");
    }
}
