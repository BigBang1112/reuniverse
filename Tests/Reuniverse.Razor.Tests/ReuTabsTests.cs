using Microsoft.AspNetCore.Components.Web;
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

    private IRenderedComponent<ReuTabs> RenderConditionalTabs(HashSet<string> visibleTabs, bool closable = false)
    {
        void BuildTabs(RenderTreeBuilder builder)
        {
            BuildTabIfVisible(builder, 0, "One", visibleTabs, closable);
            BuildTabIfVisible(builder, 1, "Two", visibleTabs, closable);
            BuildTabIfVisible(builder, 2, "Three", visibleTabs, closable);
        }

        return Render<ReuTabs>(parameters => parameters.AddChildContent(BuildTabs));
    }

    private void BuildTabIfVisible(RenderTreeBuilder builder, int sequence, string title, HashSet<string> visibleTabs, bool closable)
    {
        if (!visibleTabs.Contains(title))
        {
            return;
        }

        builder.OpenRegion(sequence);
        builder.OpenComponent<ReuTab>(0);
        builder.AddAttribute(1, nameof(ReuTab.TitleContent), (RenderFragment)(content => content.AddContent(0, title)));
        builder.AddAttribute(2, nameof(ReuTab.ChildContent), (RenderFragment)(content => content.AddContent(0, $"{title} content")));
        builder.AddAttribute(3, nameof(ReuTab.ShowCloseButton), closable);
        builder.AddAttribute(4, nameof(ReuTab.OnClosed), EventCallback.Factory.Create(this, () => visibleTabs.Remove(title)));
        builder.CloseComponent();
        builder.CloseRegion();
    }

    private static void Rerender(IRenderedComponent<ReuTabs> cut) => cut.Render(_ => { });

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
    public async Task Render_AfterTabRemoved_RemovesHeader()
    {
        // Arrange
        HashSet<string> visibleTabs = ["One", "Two"];
        var cut = RenderConditionalTabs(visibleTabs);
        visibleTabs.Remove("Two");

        // Act
        Rerender(cut);

        // Assert
        await cut.FindAll(".tab").Select(x => x.TextContent).ShouldEqualInOrderAsync("One");
    }

    [Test]
    public async Task Render_AfterSelectedTabRemoved_SelectsRemainingTab()
    {
        // Arrange
        HashSet<string> visibleTabs = ["One", "Two"];
        var cut = RenderConditionalTabs(visibleTabs);
        visibleTabs.Remove("One");

        // Act
        Rerender(cut);

        // Assert
        await Assert.That(cut.Find(".tab.selected").TextContent).IsEqualTo("Two");
    }

    [Test]
    public async Task Render_AfterSelectedMiddleTabRemoved_SelectsNextTab()
    {
        // Arrange
        HashSet<string> visibleTabs = ["One", "Two", "Three"];
        var cut = RenderConditionalTabs(visibleTabs);
        cut.FindAll(".tab")[1].Click();
        visibleTabs.Remove("Two");

        // Act
        Rerender(cut);

        // Assert
        await Assert.That(cut.Find(".tab.selected").TextContent).IsEqualTo("Three");
    }

    [Test]
    public async Task Render_AfterSelectedLastTabRemoved_SelectsPreviousTab()
    {
        // Arrange
        HashSet<string> visibleTabs = ["One", "Two", "Three"];
        var cut = RenderConditionalTabs(visibleTabs);
        cut.FindAll(".tab")[2].Click();
        visibleTabs.Remove("Three");

        // Act
        Rerender(cut);

        // Assert
        await Assert.That(cut.Find(".tab.selected").TextContent).IsEqualTo("Two");
    }

    [Test]
    public async Task Render_AfterSelectedTabRemoved_ShowsRemainingContent()
    {
        // Arrange
        HashSet<string> visibleTabs = ["One", "Two"];
        var cut = RenderConditionalTabs(visibleTabs);
        visibleTabs.Remove("One");

        // Act
        Rerender(cut);

        // Assert
        await Assert.That(cut.Markup).Contains("Two content");
    }

    [Test]
    public async Task Render_AfterOtherTabRemoved_KeepsSelection()
    {
        // Arrange
        HashSet<string> visibleTabs = ["One", "Two"];
        var cut = RenderConditionalTabs(visibleTabs);
        cut.FindAll(".tab")[1].Click();
        visibleTabs.Remove("One");

        // Act
        Rerender(cut);

        // Assert
        await Assert.That(cut.Find(".tab.selected").TextContent).IsEqualTo("Two");
    }

    [Test]
    public async Task Render_AfterAllTabsRemoved_ClearsSelection()
    {
        // Arrange
        HashSet<string> visibleTabs = ["One", "Two"];
        var cut = RenderConditionalTabs(visibleTabs);
        visibleTabs.Clear();

        // Act
        Rerender(cut);

        // Assert
        await Assert.That(cut.Instance.SelectedTab).IsNull();
    }

    [Test]
    public async Task Render_AfterAllTabsRemoved_SelectsNextAddedTab()
    {
        // Arrange
        HashSet<string> visibleTabs = ["One"];
        var cut = RenderConditionalTabs(visibleTabs);
        visibleTabs.Clear();
        Rerender(cut);
        visibleTabs.Add("Two");

        // Act
        Rerender(cut);

        // Assert
        await Assert.That(cut.Find(".tab.selected").TextContent).IsEqualTo("Two");
    }

    [Test]
    public async Task Render_ByDefault_HidesCloseButtons()
    {
        // Act
        var cut = RenderTabs();

        // Assert
        await Assert.That(cut.FindAll(".tab-close-btn").Count).IsEqualTo(0);
    }

    [Test]
    public async Task Render_WithShowCloseButton_ShowsCloseButtonOnThatTabOnly()
    {
        // Act
        var cut = Render<ReuTabs>(parameters => parameters
            .AddChildContent<ReuTab>(tab => tab.Add(x => x.TitleContent, "One"))
            .AddChildContent<ReuTab>(tab => tab.Add(x => x.TitleContent, "Two").Add(x => x.ShowCloseButton, true)));

        // Assert
        await Assert.That(cut.FindAll(".tab")[0].QuerySelectorAll(".tab-close-btn").Length).IsEqualTo(0);
        await Assert.That(cut.FindAll(".tab")[1].QuerySelectorAll(".tab-close-btn").Length).IsEqualTo(1);
    }

    [Test]
    public async Task CloseButton_WhenClicked_InvokesOnClosed()
    {
        // Arrange
        var closed = false;
        var cut = Render<ReuTabs>(parameters => parameters
            .AddChildContent<ReuTab>(tab => tab.Add(x => x.ShowCloseButton, true).Add(x => x.OnClosed, () => closed = true)));

        // Act
        cut.Find(".tab-close-btn").Click();

        // Assert
        await Assert.That(closed).IsTrue();
    }

    [Test]
    public async Task Render_WithShowCloseButton_StopsClickFromSelectingTab()
    {
        // Act
        var cut = RenderConditionalTabs(["One"], closable: true);

        // Assert
        // checked on the markup, as the button's handler re-renders the header, which stops bUnit from bubbling the click at all
        await Assert.That(cut.Find(".tab-close-btn").HasAttribute("blazor:onclick:stoppropagation")).IsTrue();
    }

    [Test]
    public async Task CloseButton_WithoutRemovingTab_KeepsTab()
    {
        // Arrange
        var cut = Render<ReuTabs>(parameters => parameters
            .AddChildContent<ReuTab>(tab => tab.Add(x => x.ShowCloseButton, true)));

        // Act
        cut.Find(".tab-close-btn").Click();

        // Assert
        await Assert.That(cut.FindAll(".tab").Count).IsEqualTo(1);
    }

    [Test]
    public async Task CloseButton_WithTabRemovedInCallback_RemovesHeader()
    {
        // Arrange
        var cut = RenderConditionalTabs(["One", "Two"], closable: true);
        cut.FindAll(".tab-close-btn")[1].Click();

        // Act
        Rerender(cut);

        // Assert
        await Assert.That(cut.FindAll(".tab").Count).IsEqualTo(1);
    }

    [Test]
    public async Task CloseButton_OnSelectedTab_SelectsNextTab()
    {
        // Arrange
        var cut = RenderConditionalTabs(["One", "Two", "Three"], closable: true);
        cut.FindAll(".tab")[1].Click();
        cut.FindAll(".tab-close-btn")[1].Click();

        // Act
        Rerender(cut);

        // Assert
        await Assert.That(cut.Instance.SelectedTab).IsSameReferenceAs(cut.FindComponents<ReuTab>()[1].Instance);
    }

    [Test]
    public async Task Dispose_WithWholeComponent_DoesNotThrow()
    {
        // Arrange
        RenderTabs();

        // Act
        var act = DisposeComponentsAsync;

        // Assert
        await Assert.That(act).ThrowsNothing();
    }
    private const string FocusIdentifier = "Blazor._internal.domWrapper.focus";

    // bUnit leaves the reference out of the markup of elements kept from an earlier render, so it is compared to the tab's own
    private async Task ShouldHaveFocusedHeaderAsync(ReuTab tab)
    {
        var target = (ElementReference)JSInterop.VerifyFocusAsyncInvoke().Arguments[0]!;
        await Assert.That(target.Id).IsEqualTo(tab.HeaderElement.Id);
    }

    private bool AnythingFocused() => JSInterop.Invocations.Any(x => x.Identifier == FocusIdentifier);

    [Test]
    public async Task Render_WithTabs_UsesTabRoles()
    {
        // Act
        var cut = RenderTabs();

        // Assert
        await Assert.That(cut.Find(".header").GetAttribute("role")).IsEqualTo("tablist");
        await cut.FindAll(".tab-title").Select(x => x.GetAttribute("role")).ShouldEqualInOrderAsync("tab", "tab");
        await Assert.That(cut.FindAll("[role=tabpanel]").Count).IsEqualTo(1);
    }

    [Test]
    public async Task Render_WithTabs_MarksOnlySelectedTab()
    {
        // Act
        var cut = RenderTabs();

        // Assert
        await cut.FindAll(".tab-title").Select(x => x.GetAttribute("aria-selected")).ShouldEqualInOrderAsync("true", "false");
    }

    [Test]
    public async Task Render_WithTabs_PutsOnlySelectedTabInTabOrder()
    {
        // Act
        var cut = RenderTabs();

        // Assert
        await cut.FindAll(".tab-title").Select(x => x.GetAttribute("tabindex")).ShouldEqualInOrderAsync("0", "-1");
    }

    [Test]
    public async Task Render_WithTabs_LinksSelectedTabAndPanel()
    {
        // Act
        var cut = RenderTabs();

        // Assert
        var header = cut.Find(".tab-title[aria-selected=true]");
        var panel = cut.Find("[role=tabpanel]");
        await Assert.That(header.GetAttribute("aria-controls")).IsEqualTo(panel.Id);
        await Assert.That(panel.GetAttribute("aria-labelledby")).IsEqualTo(header.Id);
    }

    [Test]
    public async Task Render_WithTabs_GivesEachTabItsOwnId()
    {
        // Act
        var cut = RenderTabs();

        // Assert
        var ids = cut.FindAll(".tab-title").Select(x => x.Id).ToList();
        await Assert.That(ids.Distinct().Count()).IsEqualTo(2);
    }

    [Test]
    public async Task ClickTab_WithOtherTab_MovesSelectionAndTabOrder()
    {
        // Arrange
        var cut = RenderTabs();

        // Act
        cut.FindAll(".tab")[1].Click();

        // Assert
        await cut.FindAll(".tab-title").Select(x => x.GetAttribute("aria-selected")).ShouldEqualInOrderAsync("false", "true");
        await cut.FindAll(".tab-title").Select(x => x.GetAttribute("tabindex")).ShouldEqualInOrderAsync("-1", "0");
    }

    [Test]
    [Arguments(0, "ArrowRight", "Two")]
    [Arguments(2, "ArrowRight", "One")]
    [Arguments(1, "ArrowLeft", "One")]
    [Arguments(0, "ArrowLeft", "Three")]
    [Arguments(1, "Home", "One")]
    [Arguments(1, "End", "Three")]
    public async Task KeyDown_OnTab_SelectsTab(int start, string key, string expected)
    {
        // Arrange
        var cut = RenderConditionalTabs(["One", "Two", "Three"]);
        cut.FindAll(".tab")[start].Click();

        // Act
        cut.FindAll(".tab-title")[start].KeyDown(new KeyboardEventArgs { Key = key });

        // Assert
        await Assert.That(cut.Find(".tab.selected").TextContent).IsEqualTo(expected);
    }

    [Test]
    public async Task KeyDown_OnTab_FocusesSelectedTab()
    {
        // Arrange
        var cut = RenderTabs();

        // Act
        cut.FindAll(".tab-title")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        // Assert
        await ShouldHaveFocusedHeaderAsync(cut.FindComponents<ReuTab>()[1].Instance);
    }

    [Test]
    public async Task KeyDown_WithOtherKey_KeepsSelectionAndFocus()
    {
        // Arrange
        var cut = RenderTabs();

        // Act
        cut.FindAll(".tab-title")[0].KeyDown(new KeyboardEventArgs { Key = "a" });

        // Assert
        await Assert.That(cut.Find(".tab.selected").TextContent).IsEqualTo("One");
        await Assert.That(AnythingFocused()).IsFalse();
    }

    [Test]
    public async Task Render_WithShowCloseButton_PutsOnlySelectedCloseButtonInTabOrder()
    {
        // Act
        var cut = RenderConditionalTabs(["One", "Two"], closable: true);

        // Assert
        await cut.FindAll(".tab-close-btn").Select(x => x.GetAttribute("tabindex")).ShouldEqualInOrderAsync(null, "-1");
    }

    [Test]
    public async Task Render_WithShowCloseButton_NamesCloseButtonAfterTab()
    {
        // Act
        var cut = RenderConditionalTabs(["One"], closable: true);

        // Assert
        var button = cut.Find(".tab-close-btn");
        await Assert.That(button.GetAttribute("aria-label")).IsEqualTo("Close tab");
        await Assert.That(button.GetAttribute("aria-labelledby")).IsEqualTo($"{button.Id} {cut.Find(".tab-title").Id}");
    }

    [Test]
    public async Task CloseButton_FromKeyboardOnSelectedTab_FocusesNextSelectedTab()
    {
        // Arrange
        var cut = RenderConditionalTabs(["One", "Two", "Three"], closable: true);
        cut.FindAll(".tab")[1].Click();
        cut.FindAll(".tab-close-btn")[1].Click(new MouseEventArgs { Detail = 0 });

        // Act
        Rerender(cut);

        // Assert
        await Assert.That(cut.Find(".tab.selected").TextContent).StartsWith("Three");
        await ShouldHaveFocusedHeaderAsync(cut.Instance.SelectedTab!);
    }

    [Test]
    public async Task CloseButton_FromMouse_LeavesFocusAlone()
    {
        // Arrange
        var cut = RenderConditionalTabs(["One", "Two"], closable: true);
        cut.FindAll(".tab-close-btn")[0].Click(new MouseEventArgs { Detail = 1 });

        // Act
        Rerender(cut);

        // Assert
        await Assert.That(AnythingFocused()).IsFalse();
    }
}
