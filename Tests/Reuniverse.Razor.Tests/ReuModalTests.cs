using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Reuniverse.Razor.Tests;

public class ReuModalTests : BunitContext
{
    private IRenderedComponent<ReuModal> RenderOpenModal(Action<ComponentParameterCollectionBuilder<ReuModal>>? configure = null)
    {
        return Render<ReuModal>(parameters =>
        {
            parameters.Add(x => x.IsOpen, true);
            configure?.Invoke(parameters);
        });
    }

    [Test]
    public async Task Render_WhenClosed_ShowsNothing()
    {
        // Act
        var cut = Render<ReuModal>();

        // Assert
        await Assert.That(cut.Markup.Trim()).IsEmpty();
    }

    [Test]
    public async Task Render_WhenOpen_ShowsContent()
    {
        // Act
        var cut = RenderOpenModal(parameters => parameters.AddChildContent("Body text"));

        // Assert
        await Assert.That(cut.Find(".reu-modal-body").TextContent.Trim()).IsEqualTo("Body text");
    }

    [Test]
    public async Task Render_WithSizes_AppliesSizesToBackdrop()
    {
        // Act
        var cut = RenderOpenModal(parameters => parameters.Add(x => x.MaxWidth, "300px").Add(x => x.Height, "50vh"));

        // Assert
        var style = cut.Find(".reu-modal-backdrop").GetAttribute("style");
        await Assert.That(style).Contains("--reu-modal-max-width: 300px");
        await Assert.That(style).Contains("--reu-modal-height: 50vh");
    }

    [Test]
    public async Task Render_WithTitle_ShowsTitle()
    {
        // Act
        var cut = RenderOpenModal(parameters => parameters.Add(x => x.Title, "Plain title"));

        // Assert
        await Assert.That(cut.Find(".reu-modal-title").TextContent.Trim()).IsEqualTo("Plain title");
    }

    [Test]
    public async Task Render_WithTitleAndTitleContent_PrefersTitleContent()
    {
        // Act
        var cut = RenderOpenModal(parameters => parameters
            .Add(x => x.Title, "Plain title")
            .Add(x => x.TitleContent, "Custom title"));

        // Assert
        await Assert.That(cut.Find(".reu-modal-title").TextContent.Trim()).IsEqualTo("Custom title");
    }

    [Test]
    public async Task Render_WithoutTitleOrCloseButton_HidesHeader()
    {
        // Act
        var cut = RenderOpenModal(parameters => parameters.Add(x => x.ShowCloseButton, false));

        // Assert
        await Assert.That(cut.FindAll(".reu-modal-header").Count).IsEqualTo(0);
    }

    [Test]
    public async Task Render_WithoutCloseButton_HidesCloseButton()
    {
        // Act
        var cut = RenderOpenModal(parameters => parameters
            .Add(x => x.Title, "Title")
            .Add(x => x.ShowCloseButton, false));

        // Assert
        await Assert.That(cut.FindAll(".reu-modal-close-btn").Count).IsEqualTo(0);
    }

    [Test]
    public async Task Render_WithFooterContent_ShowsFooter()
    {
        // Act
        var cut = RenderOpenModal(parameters => parameters.Add(x => x.FooterContent, "Footer text"));

        // Assert
        await Assert.That(cut.Find(".reu-modal-footer").TextContent.Trim()).IsEqualTo("Footer text");
    }

    [Test]
    public async Task Render_WithoutFooterContent_HidesFooter()
    {
        // Act
        var cut = RenderOpenModal();

        // Assert
        await Assert.That(cut.FindAll(".reu-modal-footer").Count).IsEqualTo(0);
    }

    [Test]
    public async Task CloseButton_WhenClicked_RequestsClose()
    {
        // Arrange
        bool? isOpen = null;
        var cut = RenderOpenModal(parameters => parameters.Add(x => x.IsOpenChanged, value => isOpen = value));

        // Act
        cut.Find(".reu-modal-close-btn").Click();

        // Assert
        await Assert.That(isOpen).IsFalse();
    }

    [Test]
    public async Task CloseButton_WhenClicked_InvokesOnClosed()
    {
        // Arrange
        var closed = false;
        var cut = RenderOpenModal(parameters => parameters.Add(x => x.OnClosed, () => closed = true));

        // Act
        cut.Find(".reu-modal-close-btn").Click();

        // Assert
        await Assert.That(closed).IsTrue();
    }

    [Test]
    public async Task Backdrop_WhenClicked_RequestsClose()
    {
        // Arrange
        bool? isOpen = null;
        var cut = RenderOpenModal(parameters => parameters.Add(x => x.IsOpenChanged, value => isOpen = value));

        // Act
        cut.Find(".reu-modal-backdrop").Click();

        // Assert
        await Assert.That(isOpen).IsFalse();
    }

    [Test]
    public async Task Backdrop_WithCloseOnBackdropClickOff_KeepsModalOpen()
    {
        // Arrange
        bool? isOpen = null;
        var cut = RenderOpenModal(parameters => parameters
            .Add(x => x.CloseOnBackdropClick, false)
            .Add(x => x.IsOpenChanged, value => isOpen = value));

        // Act
        cut.Find(".reu-modal-backdrop").Click();

        // Assert
        await Assert.That(isOpen).IsNull();
    }

    [Test]
    public async Task Click_InsideModal_KeepsModalOpen()
    {
        // Arrange
        bool? isOpen = null;

        void BuildContent(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "onclick", EventCallback.Factory.Create(this, () => { }));
            builder.CloseElement();
        }

        var cut = RenderOpenModal(parameters => parameters
            .Add(x => x.IsOpenChanged, value => isOpen = value)
            .AddChildContent(BuildContent));

        // Act
        cut.Find(".reu-modal-body button").Click();

        // Assert
        await Assert.That(isOpen).IsNull();
    }
}
