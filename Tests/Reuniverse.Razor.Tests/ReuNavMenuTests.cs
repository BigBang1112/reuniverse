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
        await Assert.That(cut.Find(".nav-toggle img").GetAttribute("src")).IsEqualTo("_content/Reuniverse.Razor/icons/menu.svg");
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
