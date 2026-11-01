namespace Reuniverse.Razor.Tests;

public class ReuButtonTests : BunitContext
{
    [Test]
    public async Task Render_WithoutHref_RendersButton()
    {
        // Act
        var cut = Render<ReuButton>(parameters => parameters.AddChildContent("Label"));

        // Assert
        await Assert.That(cut.Find("button").TextContent.Trim()).IsEqualTo("Label");
    }

    [Test]
    public async Task Render_WithHref_RendersLink()
    {
        // Act
        var cut = Render<ReuButton>(parameters => parameters.Add(x => x.Href, "/page"));

        // Assert
        await Assert.That(cut.Find("a").GetAttribute("href")).IsEqualTo("/page");
    }

    [Test]
    public async Task Render_WithHrefAndNavLink_RendersNavLink()
    {
        // Act
        var cut = Render<ReuButton>(parameters => parameters.Add(x => x.Href, "/page").Add(x => x.NavLink, true));

        // Assert
        await Assert.That(cut.FindComponents<Microsoft.AspNetCore.Components.Routing.NavLink>().Count).IsEqualTo(1);
    }

    [Test]
    public async Task Render_WithHrefWhenDisabled_RendersDisabledButton()
    {
        // Act
        var cut = Render<ReuButton>(parameters => parameters.Add(x => x.Href, "/page").Add(x => x.Enable, false));

        // Assert
        await Assert.That(cut.Find("button").HasAttribute("disabled")).IsTrue();
    }

    [Test]
    [Arguments(ReuButtonVariant.Blue, "reu-button-blue")]
    [Arguments(ReuButtonVariant.Yellow, "reu-button-yellow")]
    [Arguments(ReuButtonVariant.Red, "reu-button-red")]
    public async Task Render_WithVariant_AppliesVariantClass(ReuButtonVariant variant, string expectedClass)
    {
        // Act
        var cut = Render<ReuButton>(parameters => parameters.Add(x => x.Variant, variant));

        // Assert
        await Assert.That(cut.Find("button").ClassList.Contains(expectedClass)).IsTrue();
    }

    [Test]
    public async Task Render_WhenActive_AppliesActiveClass()
    {
        // Act
        var cut = Render<ReuButton>(parameters => parameters.Add(x => x.Active, true));

        // Assert
        await Assert.That(cut.Find("button").ClassList.Contains("active")).IsTrue();
    }

    [Test]
    public async Task Render_WithAdditionalAttributes_PassesThemToButton()
    {
        // Act
        var cut = Render<ReuButton>(parameters => parameters.AddUnmatched("data-test", "value"));

        // Assert
        await Assert.That(cut.Find("button").GetAttribute("data-test")).IsEqualTo("value");
    }

    [Test]
    public async Task Click_WhenEnabled_InvokesOnClick()
    {
        // Arrange
        var clicked = false;
        var cut = Render<ReuButton>(parameters => parameters.Add(x => x.OnClick, _ => clicked = true));

        // Act
        cut.Find("button").Click();

        // Assert
        await Assert.That(clicked).IsTrue();
    }
}
