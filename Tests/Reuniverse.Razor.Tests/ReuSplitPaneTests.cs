using Microsoft.AspNetCore.Components;

namespace Reuniverse.Razor.Tests;

public class ReuSplitPaneTests : BunitContext
{
    [Test]
    public async Task Render_WithContent_PutsItInPanesAroundHandle()
    {
        // Act
        var cut = Render<ReuSplitPane>(parameters => parameters
            .Add(x => x.FirstContent, "Left")
            .Add(x => x.SecondContent, "Right"));

        // Assert
        var children = cut.Find(".reu-split-pane").Children;
        await Assert.That(children.Length).IsEqualTo(3);
        await Assert.That(children[0].ClassList.Contains("reu-split-first")).IsTrue();
        await Assert.That(children[0].TextContent.Trim()).IsEqualTo("Left");
        await Assert.That(children[1].ClassList.Contains("reu-split-handle")).IsTrue();
        await Assert.That(children[2].ClassList.Contains("reu-split-second")).IsTrue();
        await Assert.That(children[2].TextContent.Trim()).IsEqualTo("Right");
    }

    [Test]
    public async Task Render_WithoutInteractivity_RendersTheSame()
    {
        // Arrange
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        // Act
        var cut = Render<ReuSplitPane>();

        // Assert
        await Assert.That(cut.FindAll(".reu-split-handle").Count).IsEqualTo(1);
    }

    [Test]
    public async Task Render_Default_SplitsInHalfSideBySide()
    {
        // Act
        var cut = Render<ReuSplitPane>();

        // Assert
        var pane = cut.Find(".reu-split-pane");
        await Assert.That(pane.ClassList.Contains("vertical")).IsFalse();
        await Assert.That(pane.GetAttribute("style")).IsEqualTo("--reu-split-size: 50%;");
        await Assert.That(pane.GetAttribute("data-reu-split-size")).IsEqualTo("50%");
        await Assert.That(pane.HasAttribute("data-reu-split-key")).IsFalse();
    }

    [Test]
    public async Task Render_WithSizes_SetsThemAsVariables()
    {
        // Act
        var cut = Render<ReuSplitPane>(parameters => parameters
            .Add(x => x.Size, "250px")
            .Add(x => x.FirstMinSize, "100px")
            .Add(x => x.SecondMinSize, "20%"));

        // Assert
        var pane = cut.Find(".reu-split-pane");
        await Assert.That(pane.GetAttribute("style")).IsEqualTo("--reu-split-size: 250px; --reu-split-first-min: 100px; --reu-split-second-min: 20%;");
        await Assert.That(pane.GetAttribute("data-reu-split-size")).IsEqualTo("250px");
    }

    [Test]
    public async Task Render_WithStyle_KeepsItAlongsideVariables()
    {
        // Act
        var cut = Render<ReuSplitPane>(parameters => parameters.AddUnmatched("style", "height: 400px"));

        // Assert
        await Assert.That(cut.Find(".reu-split-pane").GetAttribute("style")).IsEqualTo("--reu-split-size: 50%; height: 400px");
    }

    [Test]
    public async Task Render_WithStorageKey_SetsIt()
    {
        // Act
        var cut = Render<ReuSplitPane>(parameters => parameters.Add(x => x.StorageKey, "explorer"));

        // Assert
        await Assert.That(cut.Find(".reu-split-pane").GetAttribute("data-reu-split-key")).IsEqualTo("explorer");
    }

    [Test]
    [Arguments(false, "vertical")]
    [Arguments(true, "horizontal")]
    public async Task Render_Handle_IsFocusableSeparatorAcrossPanes(bool vertical, string orientation)
    {
        // Act
        var cut = Render<ReuSplitPane>(parameters => parameters.Add(x => x.Vertical, vertical));

        // Assert
        var handle = cut.Find(".reu-split-handle");
        await Assert.That(handle.GetAttribute("role")).IsEqualTo("separator");
        await Assert.That(handle.GetAttribute("tabindex")).IsEqualTo("0");
        await Assert.That(handle.GetAttribute("aria-orientation")).IsEqualTo(orientation);
        await Assert.That(handle.GetAttribute("aria-label")).IsEqualTo("Resize panes");
        await Assert.That(handle.GetAttribute("aria-controls")).IsEqualTo(cut.Find(".reu-split-first").Id);
    }

    [Test]
    public async Task Render_Vertical_AddsClass()
    {
        // Act
        var cut = Render<ReuSplitPane>(parameters => parameters.Add(x => x.Vertical, true));

        // Assert
        await Assert.That(cut.Find(".reu-split-pane").ClassList.Contains("vertical")).IsTrue();
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task Render_WithBorder_AddsClassOnlyWhenOn(bool border)
    {
        // Act
        var cut = Render<ReuSplitPane>(parameters => parameters.Add(x => x.Border, border));

        // Assert
        await Assert.That(cut.Find(".reu-split-pane").ClassList.Contains("bordered")).IsEqualTo(border);
    }

    [Test]
    public async Task Render_Default_HasBorder()
    {
        // Act
        var cut = Render<ReuSplitPane>();

        // Assert
        await Assert.That(cut.Find(".reu-split-pane").ClassList.Contains("bordered")).IsTrue();
    }

    [Test]
    public async Task Render_Twice_UsesDifferentPaneIds()
    {
        // Act
        var first = Render<ReuSplitPane>();
        var second = Render<ReuSplitPane>();

        // Assert
        await Assert.That(first.Find(".reu-split-first").Id).IsNotEqualTo(second.Find(".reu-split-first").Id);
    }

    [Test]
    public async Task Render_WithHandleLabel_UsesIt()
    {
        // Act
        var cut = Render<ReuSplitPane>(parameters => parameters.Add(x => x.HandleLabel, "Resize the file list"));

        // Assert
        await Assert.That(cut.Find(".reu-split-handle").GetAttribute("aria-label")).IsEqualTo("Resize the file list");
    }

    [Test]
    public async Task Render_WithCssClassAndAttributes_PassesThemToPane()
    {
        // Act
        var cut = Render<ReuSplitPane>(parameters => parameters
            .Add(x => x.CssClass, "custom")
            .AddUnmatched("id", "split"));

        // Assert
        var pane = cut.Find(".reu-split-pane");
        await Assert.That(pane.ClassList.Contains("custom")).IsTrue();
        await Assert.That(pane.Id).IsEqualTo("split");
    }
}
