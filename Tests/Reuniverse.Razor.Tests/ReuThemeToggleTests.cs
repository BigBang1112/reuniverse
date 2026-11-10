using Microsoft.AspNetCore.Components;

namespace Reuniverse.Razor.Tests;

public class ReuThemeToggleTests : BunitContext
{
    [Test]
    public async Task Render_Default_RendersLightbulbButtonThatTogglesTheme()
    {
        // Act
        var cut = Render<ReuThemeToggle>();

        // Assert
        var button = cut.Find("button");
        await Assert.That(button.GetAttribute("type")).IsEqualTo("button");
        await Assert.That(button.GetAttribute("onclick")).IsEqualTo("reuTheme.toggle()");
        await Assert.That(button.ClassList.Contains("reu-theme-toggle")).IsTrue();
        await Assert.That(button.ClassList.Contains("reu-button")).IsFalse();
        await Assert.That(button.QuerySelector(".reu-theme-icon")!.GetAttribute("aria-hidden")).IsEqualTo("true");
    }

    [Test]
    public async Task Render_WithoutInteractivity_StillTogglesTheme()
    {
        // Arrange
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        // Act
        var cut = Render<ReuThemeToggle>();

        // Assert
        await Assert.That(cut.Find("button").GetAttribute("onclick")).IsEqualTo("reuTheme.toggle()");
    }

    [Test]
    public async Task Render_Default_HasTooltipAndLabel()
    {
        // Act
        var cut = Render<ReuThemeToggle>();

        // Assert
        var button = cut.Find("button");
        await Assert.That(button.GetAttribute("data-reu-tooltip")).IsEqualTo("Switch theme");
        await Assert.That(button.GetAttribute("aria-label")).IsEqualTo("Switch theme");
    }

    [Test]
    public async Task Render_WithTooltip_UsesItForTooltipAndLabel()
    {
        // Act
        var cut = Render<ReuThemeToggle>(parameters => parameters.Add(x => x.Tooltip, "Theme"));

        // Assert
        var button = cut.Find("button");
        await Assert.That(button.GetAttribute("data-reu-tooltip")).IsEqualTo("Theme");
        await Assert.That(button.GetAttribute("aria-label")).IsEqualTo("Theme");
    }

    [Test]
    public async Task Render_WithNullTooltip_KeepsLabelButNoTooltip()
    {
        // Act
        var cut = Render<ReuThemeToggle>(parameters => parameters.Add(x => x.Tooltip, null));

        // Assert
        var button = cut.Find("button");
        await Assert.That(button.HasAttribute("data-reu-tooltip")).IsFalse();
        await Assert.That(button.GetAttribute("aria-label")).IsEqualTo("Switch theme");
    }

    [Test]
    public async Task Render_WithSize_SizesIcon()
    {
        // Act
        var cut = Render<ReuThemeToggle>(parameters => parameters.Add(x => x.Size, 32));

        // Assert
        var style = cut.Find(".reu-theme-icon").GetAttribute("style");
        await Assert.That(style).Contains("width: 32px");
        await Assert.That(style).Contains("height: 32px");
    }

    [Test]
    public async Task Render_WithCssClassAndAttributes_PassesThemToButton()
    {
        // Act
        var cut = Render<ReuThemeToggle>(parameters => parameters
            .Add(x => x.CssClass, "custom")
            .AddUnmatched("id", "toggle"));

        // Assert
        var button = cut.Find("button");
        await Assert.That(button.ClassList.Contains("custom")).IsTrue();
        await Assert.That(button.Id).IsEqualTo("toggle");
    }
}
