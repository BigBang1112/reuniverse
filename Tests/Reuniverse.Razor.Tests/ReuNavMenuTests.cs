namespace Reuniverse.Razor.Tests;

public class ReuNavMenuTests : BunitContext
{
    [Test]
    public async Task Render_WithBrand_ShowsBrand()
    {
        // Act
        var cut = Render<ReuNavMenu>(parameters => parameters.Add(x => x.Brand, "Reuniverse").AddChildContent(""));

        // Assert
        await Assert.That(cut.Find(".brand").TextContent).IsEqualTo("Reuniverse");
    }

    [Test]
    public async Task Render_WithoutBurgerImage_UsesBundledIcon()
    {
        // Act
        var cut = Render<ReuNavMenu>(parameters => parameters.Add(x => x.Brand, "Reuniverse").AddChildContent(""));

        // Assert
        await Assert.That(cut.Find(".nav-toggle-icon").GetAttribute("style")).Contains("_content/Reuniverse.Razor/icons/menu.svg");
    }

    [Test]
    public async Task Render_ByDefault_NamesBurgerForScreenReaders()
    {
        // Act
        var cut = Render<ReuNavMenu>(parameters => parameters.Add(x => x.Brand, "Reuniverse").AddChildContent(""));

        // Assert
        await Assert.That(cut.Find(".nav-toggle input").GetAttribute("aria-label")).IsEqualTo("Menu");
    }

    [Test]
    public async Task Render_Twice_HasNoIdsToCollide()
    {
        // Act
        var first = Render<ReuNavMenu>(parameters => parameters.Add(x => x.Brand, "First").AddChildContent(""));

        // Assert
        await Assert.That(first.Find(".nav-toggle input").HasAttribute("id")).IsFalse();
        await Assert.That(first.Find(".nav-toggle").HasAttribute("for")).IsFalse();
    }

    [Test]
    public async Task Render_WithIsAbsolute_AppliesAbsoluteClass()
    {
        // Act
        var cut = Render<ReuNavMenu>(parameters => parameters
            .Add(x => x.Brand, "Reuniverse")
            .Add(x => x.IsAbsolute, true)
            .AddChildContent(""));

        // Assert
        await Assert.That(cut.Find(".navbar").ClassList.Contains("absolute")).IsTrue();
    }

    [Test]
    public async Task Render_WithBurgerImage_UsesCustomIcon()
    {
        // Act
        var cut = Render<ReuNavMenu>(parameters => parameters
            .Add(x => x.Brand, "Reuniverse")
            .Add(x => x.BurgerImageSrc, "/burger.svg")
            .AddChildContent(""));

        // Assert
        await Assert.That(cut.Find(".nav-toggle img").GetAttribute("src")).IsEqualTo("/burger.svg");
    }
}
