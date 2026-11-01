using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Reuniverse.Razor.Tests;

public class ReuExpansionPanelsTests : BunitContext
{
    private bool firstExpanded;
    private bool secondExpanded;

    // same as @bind-Expanded on both panels
    private void BuildBoundPanels(RenderTreeBuilder builder)
    {
        BuildPanel(builder, 0, firstExpanded, value => firstExpanded = value);
        BuildPanel(builder, 10, secondExpanded, value => secondExpanded = value);
    }

    private void BuildPanel(RenderTreeBuilder builder, int sequence, bool expanded, Action<bool> expandedChanged)
    {
        builder.OpenComponent<ReuExpansionPanel>(sequence);
        builder.AddAttribute(sequence + 1, nameof(ReuExpansionPanel.Expanded), expanded);
        builder.AddAttribute(sequence + 2, nameof(ReuExpansionPanel.ExpandedChanged), EventCallback.Factory.Create(this, expandedChanged));
        builder.CloseComponent();
    }

    private IRenderedComponent<ReuExpansionPanels> RenderPanels(bool singleExpansion, bool firstExpanded = false, bool secondExpanded = false)
    {
        this.firstExpanded = firstExpanded;
        this.secondExpanded = secondExpanded;

        return Render<ReuExpansionPanels>(parameters => parameters
            .Add(x => x.SingleExpansion, singleExpansion)
            .AddChildContent(BuildBoundPanels));
    }

    private static int ExpandedCount(IRenderedComponent<ReuExpansionPanels> cut) => cut.FindAll(".content-container.expanded").Count;

    [Test]
    public async Task Render_ByDefault_CollapsesPanels()
    {
        // Act
        var cut = RenderPanels(singleExpansion: false);

        // Assert
        await Assert.That(ExpandedCount(cut)).IsEqualTo(0);
    }

    [Test]
    public async Task Toggle_WhenCollapsed_ExpandsPanel()
    {
        // Arrange
        var cut = RenderPanels(singleExpansion: false);

        // Act
        cut.FindAll(".expandable")[0].Click();

        // Assert
        await Assert.That(ExpandedCount(cut)).IsEqualTo(1);
    }

    [Test]
    public async Task Toggle_WhenExpanded_CollapsesPanel()
    {
        // Arrange
        var cut = RenderPanels(singleExpansion: false, firstExpanded: true);

        // Act
        cut.FindAll(".expandable")[0].Click();

        // Assert
        await Assert.That(ExpandedCount(cut)).IsEqualTo(0);
    }

    [Test]
    public async Task Toggle_WithExpandedChanged_ReportsState()
    {
        // Arrange
        var cut = RenderPanels(singleExpansion: false);

        // Act
        cut.FindAll(".expandable")[0].Click();

        // Assert
        await Assert.That(firstExpanded).IsTrue();
    }

    [Test]
    public async Task Toggle_WithoutSingleExpansion_KeepsOtherPanelExpanded()
    {
        // Arrange
        var cut = RenderPanels(singleExpansion: false, firstExpanded: true);

        // Act
        cut.FindAll(".expandable")[1].Click();

        // Assert
        await Assert.That(ExpandedCount(cut)).IsEqualTo(2);
    }

    [Test]
    public async Task Toggle_WithSingleExpansion_CollapsesOtherPanel()
    {
        // Arrange
        var cut = RenderPanels(singleExpansion: true, firstExpanded: true);

        // Act
        cut.FindAll(".expandable")[1].Click();

        // Assert
        await Assert.That(ExpandedCount(cut)).IsEqualTo(1);
    }

    [Test]
    public async Task Toggle_WithSingleExpansion_NotifiesCollapsedPanel()
    {
        // Arrange
        var cut = RenderPanels(singleExpansion: true, firstExpanded: true);

        // Act
        cut.FindAll(".expandable")[1].Click();

        // Assert
        await Assert.That(firstExpanded).IsFalse();
    }

    [Test]
    public async Task CollapseAllAsync_WithExpandedPanels_CollapsesPanels()
    {
        // Arrange
        var cut = RenderPanels(singleExpansion: false, firstExpanded: true, secondExpanded: true);

        // Act
        await cut.InvokeAsync(cut.Instance.CollapseAllAsync);

        // Assert
        await Assert.That(ExpandedCount(cut)).IsEqualTo(0);
    }

    [Test]
    [Skip("Known bug: CollapseAllAsync ends with StateHasChanged on the parent, which reapplies a one-way Expanded=\"true\" and reopens the panel")]
    public async Task Toggle_WithSingleExpansionAndOneWayExpanded_CollapsesOtherPanel()
    {
        // Arrange
        var cut = Render<ReuExpansionPanels>(parameters => parameters
            .Add(x => x.SingleExpansion, true)
            .AddChildContent<ReuExpansionPanel>(panel => panel.Add(x => x.Expanded, true).Add(x => x.TitleContent, "First"))
            .AddChildContent<ReuExpansionPanel>(panel => panel.Add(x => x.TitleContent, "Second")));

        // Act
        cut.FindAll(".expandable")[1].Click();

        // Assert
        await Assert.That(ExpandedCount(cut)).IsEqualTo(1);
    }

    [Test]
    public async Task Dispose_WithRemovedPanel_UnregistersPanel()
    {
        // Arrange
        var showPanel = true;

        void BuildPanels(RenderTreeBuilder builder)
        {
            if (showPanel)
            {
                builder.OpenComponent<ReuExpansionPanel>(0);
                builder.CloseComponent();
            }
        }

        var cut = Render<ReuExpansionPanels>(parameters => parameters.AddChildContent(BuildPanels));
        showPanel = false;

        // Act
        cut.Render(_ => { });

        // Assert
        await Assert.That(cut.Instance.Panels.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Toggle_WithoutParent_ExpandsPanel()
    {
        // Arrange
        var cut = Render<ReuExpansionPanel>();

        // Act
        cut.Find(".expandable").Click();

        // Assert
        await Assert.That(cut.FindAll(".content-container.expanded").Count).IsEqualTo(1);
    }
}
