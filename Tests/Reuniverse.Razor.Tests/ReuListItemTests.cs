namespace Reuniverse.Razor.Tests;

public class ReuListItemTests : BunitContext
{
    [Test]
    public async Task Render_WithoutHref_RendersButton()
    {
        // Act
        var cut = Render<ReuListItem>(parameters => parameters.AddChildContent("Label"));

        // Assert
        await Assert.That(cut.Find("button").TextContent.Trim()).IsEqualTo("Label");
    }

    [Test]
    public async Task Render_WithHref_RendersLink()
    {
        // Act
        var cut = Render<ReuListItem>(parameters => parameters.Add(x => x.Href, "/page"));

        // Assert
        await Assert.That(cut.Find("a").GetAttribute("href")).IsEqualTo("/page");
    }

    [Test]
    public async Task Render_WithHrefAndNavLink_RendersNavLink()
    {
        // Act
        var cut = Render<ReuListItem>(parameters => parameters.Add(x => x.Href, "/page").Add(x => x.NavLink, true));

        // Assert
        await Assert.That(cut.FindComponents<Microsoft.AspNetCore.Components.Routing.NavLink>().Count).IsEqualTo(1);
    }

    [Test]
    public async Task Render_WhenDisabled_DisablesButton()
    {
        // Act
        var cut = Render<ReuListItem>(parameters => parameters.Add(x => x.Enable, false));

        // Assert
        await Assert.That(cut.Find("button").HasAttribute("disabled")).IsTrue();
    }

    [Test]
    public async Task Click_WhenEnabled_InvokesOnClick()
    {
        // Arrange
        var clicked = false;
        var cut = Render<ReuListItem>(parameters => parameters.Add(x => x.OnClick, _ => clicked = true));

        // Act
        cut.Find("button").Click();

        // Assert
        await Assert.That(clicked).IsTrue();
    }
}
