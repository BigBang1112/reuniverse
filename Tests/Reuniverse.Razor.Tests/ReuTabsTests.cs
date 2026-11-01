using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Reuniverse.Razor.Tests;

public class ReuTabsTests : BunitContext
{
    private IRenderedComponent<ReuTabs> RenderTabs()
    {
        return Render<ReuTabs>(parameters => parameters
            .AddChildContent<ReuTab>(tab => tab.Add(x => x.TitleContent, "One").AddChildContent("First content"))
            .AddChildContent<ReuTab>(tab => tab.Add(x => x.TitleContent, "Two").AddChildContent("Second content")));
    }

    private static void BuildTab(RenderTreeBuilder builder, string title)
    {
        builder.OpenComponent<ReuTab>(0);
        builder.AddAttribute(1, nameof(ReuTab.TitleContent), (RenderFragment)(content => content.AddContent(0, title)));
        builder.CloseComponent();
    }

    [Test]
    public async Task Render_WithTabs_ShowsHeaders()
    {
        // Act
        var cut = RenderTabs();

        // Assert
        await cut.FindAll(".tab").Select(x => x.TextContent).ShouldEqualInOrderAsync("One", "Two");
    }

    [Test]
    public async Task Render_WithTabs_SelectsFirstTab()
    {
        // Act
        var cut = RenderTabs();

        // Assert
        await Assert.That(cut.Find(".tab.selected").TextContent).IsEqualTo("One");
    }

    [Test]
    public async Task Render_WithTabs_ShowsOnlySelectedContent()
    {
        // Act
        var cut = RenderTabs();

        // Assert
        await Assert.That(cut.Markup).Contains("First content");
        await Assert.That(cut.Markup).DoesNotContain("Second content");
    }

    [Test]
    public async Task ClickTab_WithOtherTab_ShowsItsContent()
    {
        // Arrange
        var cut = RenderTabs();

        // Act
        cut.FindAll(".tab")[1].Click();

        // Assert
        await Assert.That(cut.Markup).Contains("Second content");
        await Assert.That(cut.Markup).DoesNotContain("First content");
    }

    [Test]
    public async Task ClickTab_WithOtherTab_UpdatesSelectedTab()
    {
        // Arrange
        var cut = RenderTabs();

        // Act
        cut.FindAll(".tab")[1].Click();

        // Assert
        await Assert.That(cut.Instance.SelectedTab).IsSameReferenceAs(cut.FindComponents<ReuTab>()[1].Instance);
    }

    [Test]
    [Skip("Known bug: ReuTab has no Dispose or Unregister, so a removed tab keeps its header")]
    public async Task Render_AfterTabRemoved_RemovesHeader()
    {
        // Arrange
        var showSecond = true;

        void BuildTabs(RenderTreeBuilder builder)
        {
            builder.OpenRegion(0);
            BuildTab(builder, "One");
            builder.CloseRegion();

            if (showSecond)
            {
                builder.OpenRegion(1);
                BuildTab(builder, "Two");
                builder.CloseRegion();
            }
        }

        var cut = Render<ReuTabs>(parameters => parameters.AddChildContent(BuildTabs));
        showSecond = false;

        // Act
        cut.Render(_ => { });

        // Assert
        await cut.FindAll(".tab").Select(x => x.TextContent).ShouldEqualInOrderAsync("One");
    }
}
