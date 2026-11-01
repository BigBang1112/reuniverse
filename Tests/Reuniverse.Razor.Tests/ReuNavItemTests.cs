using Microsoft.AspNetCore.Components.Routing;

namespace Reuniverse.Razor.Tests;

public class ReuNavItemTests : BunitContext
{
    [Test]
    public async Task Render_ByDefault_RendersNavLink()
    {
        // Act
        var cut = Render<ReuNavItem>(parameters => parameters.Add(x => x.Href, "/page"));

        // Assert
        await Assert.That(cut.FindComponents<NavLink>().Count).IsEqualTo(1);
    }

    [Test]
    public async Task Render_WithNavLinkOff_RendersPlainLink()
    {
        // Act
        var cut = Render<ReuNavItem>(parameters => parameters.Add(x => x.Href, "/page").Add(x => x.IsNavLink, false));

        // Assert
        await Assert.That(cut.FindComponents<NavLink>().Count).IsEqualTo(0);
    }

    [Test]
    public async Task Render_WithIconType_UsesBundledIcon()
    {
        // Act
        var cut = Render<ReuNavItem>(parameters => parameters.Add(x => x.IconType, IconType.GitHub));

        // Assert
        await Assert.That(cut.Find(".icon").GetAttribute("style")).Contains("_content/Reuniverse.Razor/icons/github.svg");
    }

    [Test]
    public async Task Render_WithIconSrc_UsesCustomIcon()
    {
        // Act
        var cut = Render<ReuNavItem>(parameters => parameters.Add(x => x.IconSrc, "/custom.svg"));

        // Assert
        await Assert.That(cut.Find("img").GetAttribute("src")).IsEqualTo("/custom.svg");
    }

    [Test]
    public async Task Render_WithIconType_DescribesIconForScreenReaders()
    {
        // Act
        var cut = Render<ReuNavItem>(parameters => parameters.Add(x => x.IconType, IconType.GitHub).Add(x => x.Name, "GitHub"));

        // Assert
        await Assert.That(cut.Find(".icon").GetAttribute("role")).IsEqualTo("img");
        await Assert.That(cut.Find(".icon").GetAttribute("aria-label")).IsEqualTo("GitHub");
    }

    [Test]
    public async Task Render_WithIconSize_AppliesSize()
    {
        // Act
        var cut = Render<ReuNavItem>(parameters => parameters
            .Add(x => x.IconType, IconType.GitHub)
            .Add(x => x.IconWidth, 28)
            .Add(x => x.IconHeight, 20));

        // Assert
        await Assert.That(cut.Find(".icon").GetAttribute("style")).Contains("--icon-width: 28px; --icon-height: 20px;");
    }

    [Test]
    public async Task Render_WithoutIcon_KeepsLabelVisibleInIconLayout()
    {
        // Act
        var cut = Render<ReuNavItem>(parameters => parameters.Add(x => x.Name, "Home"));

        // Assert
        await Assert.That(cut.Find(".nav-label").ClassList.Contains("nav-label-only")).IsTrue();
    }

    [Test]
    public async Task Render_WithIcon_LetsLayoutHideLabel()
    {
        // Act
        var cut = Render<ReuNavItem>(parameters => parameters.Add(x => x.Name, "Home").Add(x => x.IconType, IconType.Home));

        // Assert
        await Assert.That(cut.Find(".nav-label").ClassList.Contains("nav-label-only")).IsFalse();
    }

    [Test]
    public async Task Render_WithChildContent_KeepsItemLink()
    {
        // Act
        var cut = RenderSubmenu();

        // Assert
        await Assert.That(cut.Find(".nav-submenu > .nav-item > a").GetAttribute("href")).Contains("tools");
    }

    [Test]
    public async Task Render_WithChildContent_PutsItemsInPanel()
    {
        // Act
        var cut = RenderSubmenu();

        // Assert
        await Assert.That(cut.Find(".nav-submenu > .nav-submenu-items a").GetAttribute("href")).Contains("converter");
    }

    [Test]
    public async Task Render_WithChildContent_KeepsAttributesOffPanel()
    {
        // Act
        var cut = Render<ReuNavItem>(parameters => parameters
            .Add(x => x.Name, "Tools")
            .AddUnmatched("data-test", "tools")
            .AddChildContent<ReuNavItem>(child => child.Add(x => x.Name, "Converter")));

        // Assert
        await Assert.That(cut.Find("[data-test]").QuerySelector(".nav-submenu-items")).IsNull();
    }

    [Test]
    public async Task Render_WithChildContentAndHref_LeavesTabOrderAlone()
    {
        // Act
        var cut = RenderSubmenu();

        // Assert
        await Assert.That(cut.Find(".nav-submenu > .nav-item > a").HasAttribute("tabindex")).IsFalse();
    }

    [Test]
    public async Task Render_WithChildContentWithoutHref_CanBeFocused()
    {
        // Act
        var cut = Render<ReuNavItem>(parameters => parameters
            .Add(x => x.Name, "Tools")
            .AddChildContent<ReuNavItem>(child => child.Add(x => x.Name, "Converter")));

        // Assert
        await Assert.That(cut.Find(".nav-submenu > .nav-item > a").GetAttribute("tabindex")).IsEqualTo("0");
    }

    [Test]
    public async Task Render_WithChildContentAsPlainLink_CanBeFocused()
    {
        // Act
        var cut = Render<ReuNavItem>(parameters => parameters
            .Add(x => x.Name, "Tools")
            .Add(x => x.IsNavLink, false)
            .AddChildContent<ReuNavItem>(child => child.Add(x => x.Name, "Converter")));

        // Assert
        await Assert.That(cut.Find(".nav-submenu > .nav-item > a").GetAttribute("tabindex")).IsEqualTo("0");
    }

    [Test]
    public async Task Render_WithoutHref_IsNotActive()
    {
        // Act
        var cut = Render<ReuNavItem>(parameters => parameters
            .Add(x => x.Name, "Tools")
            .AddChildContent<ReuNavItem>(child => child.Add(x => x.Name, "Converter").Add(x => x.Href, "/converter")));

        // Assert
        await Assert.That(cut.FindAll("a.active").Count).IsEqualTo(0);
    }

    [Test]
    public async Task Render_WithoutChildContent_RendersNoSubmenu()
    {
        // Act
        var cut = Render<ReuNavItem>(parameters => parameters.Add(x => x.Name, "Home").Add(x => x.Href, "/page"));

        // Assert
        await Assert.That(cut.FindAll(".nav-submenu").Count).IsEqualTo(0);
    }

    [Test]
    public async Task Render_WithoutChildContent_LeavesTabOrderAlone()
    {
        // Act
        var cut = Render<ReuNavItem>(parameters => parameters.Add(x => x.Name, "Home"));

        // Assert
        await Assert.That(cut.Find("a").HasAttribute("tabindex")).IsFalse();
    }

    [Test]
    public async Task Render_WithoutInteractivity_StillRendersSubmenu()
    {
        // Arrange
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Static", isInteractive: false));

        // Act
        var cut = RenderSubmenu();

        // Assert
        await Assert.That(cut.FindAll(".nav-submenu").Count).IsEqualTo(1);
    }

    private IRenderedComponent<ReuNavItem> RenderSubmenu()
    {
        return Render<ReuNavItem>(parameters => parameters
            .Add(x => x.Name, "Tools")
            .Add(x => x.Href, "/tools")
            .AddChildContent<ReuNavItem>(child => child.Add(x => x.Name, "Converter").Add(x => x.Href, "/converter")));
    }

    [Test]
    public async Task Render_WithoutIcon_LeavesIconOut()
    {
        // Act
        var cut = Render<ReuNavItem>(parameters => parameters.Add(x => x.Name, "Home"));

        // Assert
        await Assert.That(cut.FindAll(".icon").Count).IsEqualTo(0);
    }

    [Test]
    [Arguments(IconType.Discord, "reu-discord")]
    [Arguments(IconType.NuGet, "reu-nuget")]
    public async Task Render_WithBrandIcon_AppliesBrandClass(IconType iconType, string expectedClass)
    {
        // Act
        var cut = Render<ReuNavItem>(parameters => parameters.Add(x => x.IconType, iconType).Add(x => x.Href, "/page"));

        // Assert
        await Assert.That(cut.Find("a").ClassList.Contains(expectedClass)).IsTrue();
    }

    [Test]
    public async Task Render_WithIconOnly_MarksNameAsLabel()
    {
        // Act
        var cut = Render<ReuNavItem>(parameters => parameters.Add(x => x.Name, "Home").Add(x => x.IconOnly, true));

        // Assert
        await Assert.That(cut.Find(".nav-item-label").TextContent).IsEqualTo("Home");
    }

    [Test]
    public async Task Render_WithoutTitle_UsesNameAsTitle()
    {
        // Act
        var cut = Render<ReuNavItem>(parameters => parameters.Add(x => x.Name, "Home"));

        // Assert
        await Assert.That(cut.Find(".nav-item").GetAttribute("title")).IsEqualTo("Home");
    }

    [Test]
    public async Task Render_WithTitle_PrefersTitle()
    {
        // Act
        var cut = Render<ReuNavItem>(parameters => parameters.Add(x => x.Name, "Home").Add(x => x.Title, "Go home"));

        // Assert
        await Assert.That(cut.Find(".nav-item").GetAttribute("title")).IsEqualTo("Go home");
    }
}
