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
        await Assert.That(cut.Find("img").GetAttribute("src")).IsEqualTo("_content/Reuniverse.Razor/icons/github.svg");
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
    public async Task Render_WithoutIcon_LeavesIconOut()
    {
        // Act
        var cut = Render<ReuNavItem>(parameters => parameters.Add(x => x.Name, "Home"));

        // Assert
        await Assert.That(cut.FindAll("img").Count).IsEqualTo(0);
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
