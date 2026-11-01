using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Reuniverse.Razor.Tests;

public class ReuTooltipTests : ReuObjectTreeTestBase
{
    private const string TooltipAttribute = "data-reu-tooltip";

    private class Model
    {
        public string? Nullable { get; set; }
        public Model? Child { get; set; }
    }

    [Test]
    public async Task Render_WithText_SetsTooltipOnContent()
    {
        // Act
        var cut = Render<ReuTooltip>(parameters => parameters
            .Add(x => x.Text, "Hint")
            .AddChildContent("Content"));

        // Assert
        await Assert.That(cut.Find("span").GetAttribute(TooltipAttribute)).IsEqualTo("Hint");
        await Assert.That(cut.Find("span").TextContent).IsEqualTo("Content");
    }

    [Test]
    public async Task Render_WithoutText_LeavesAttributeOut()
    {
        // Act
        var cut = Render<ReuTooltip>(parameters => parameters.AddChildContent("Content"));

        // Assert
        await Assert.That(cut.Find("span").HasAttribute(TooltipAttribute)).IsFalse();
    }

    [Test]
    public async Task Render_WithAdditionalAttributes_PassesThemToElement()
    {
        // Act
        var cut = Render<ReuTooltip>(parameters => parameters.AddUnmatched("class", "custom"));

        // Assert
        await Assert.That(cut.Find("span").ClassList.Contains("custom")).IsTrue();
    }

    [Test]
    public async Task Render_WithOldIsGlobalParameter_DoesNotThrow()
    {
        // Act
        var act = () => Render<ReuTooltip>(parameters => parameters.AddUnmatched("IsGlobal", true));

        // Assert
        await Assert.That(act).ThrowsNothing();
    }

    [Test]
    public async Task Render_WithoutInteractivity_StillSetsTooltip()
    {
        // Arrange
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        // Act
        var cut = Render<ReuButton>(parameters => parameters.Add(x => x.Tooltip, "Hint"));

        // Assert
        await Assert.That(cut.Find("button").GetAttribute(TooltipAttribute)).IsEqualTo("Hint");
    }

    [Test]
    public async Task Render_ButtonWithoutTooltip_LeavesAttributeOut()
    {
        // Act
        var cut = Render<ReuButton>();

        // Assert
        await Assert.That(cut.Find("button").HasAttribute(TooltipAttribute)).IsFalse();
    }

    [Test]
    public async Task Render_DisabledButtonWithTooltip_KeepsTooltip()
    {
        // Act
        var cut = Render<ReuButton>(parameters => parameters.Add(x => x.Tooltip, "Hint").Add(x => x.Enable, false));

        // Assert
        await Assert.That(cut.Find("button").GetAttribute(TooltipAttribute)).IsEqualTo("Hint");
    }

    [Test]
    public async Task Render_ButtonWithHref_SetsTooltipOnLink()
    {
        // Act
        var cut = Render<ReuButton>(parameters => parameters.Add(x => x.Tooltip, "Hint").Add(x => x.Href, "/page"));

        // Assert
        await Assert.That(cut.Find("a").GetAttribute(TooltipAttribute)).IsEqualTo("Hint");
    }

    [Test]
    public async Task Render_ButtonWithNavLink_SetsTooltipOnLink()
    {
        // Act
        var cut = Render<ReuButton>(parameters => parameters
            .Add(x => x.Tooltip, "Hint")
            .Add(x => x.Href, "/page")
            .Add(x => x.NavLink, true));

        // Assert
        await Assert.That(cut.FindComponent<NavLink>().Find("a").GetAttribute(TooltipAttribute)).IsEqualTo("Hint");
    }

    [Test]
    public async Task Render_CheckboxWithTooltip_SetsTooltipOnLabel()
    {
        // Act
        var cut = Render<ReuCheckbox>(parameters => parameters.Add(x => x.Tooltip, "Hint"));

        // Assert
        await Assert.That(cut.Find("label").GetAttribute(TooltipAttribute)).IsEqualTo("Hint");
    }

    [Test]
    public async Task Render_UploadButtonWithTooltip_SetsTooltipOnButton()
    {
        // Arrange
        JSInterop.SetupModule("./_content/Reuniverse.Razor/ReuUploadButton.razor.js");
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        // Act
        var cut = Render<ReuUploadButton>(parameters => parameters.Add(x => x.Tooltip, "Hint"));

        // Assert
        await Assert.That(cut.Find("button").GetAttribute(TooltipAttribute)).IsEqualTo("Hint");
    }

    [Test]
    public async Task Render_ObjectTreeActions_DescribeTheirAction()
    {
        // Act
        var cut = RenderObjectTree(new Model());

        // Assert
        await Assert.That(cut.MemberRow(nameof(Model.Child)).QuerySelector(".box-value.clickable")!.GetAttribute(TooltipAttribute)).IsEqualTo("Create a new instance");
        await Assert.That(cut.MemberRow(nameof(Model.Nullable)).QuerySelector(".box-value.clickable")!.GetAttribute(TooltipAttribute)).IsEqualTo("Set to empty string");
    }

    [Test]
    public async Task Render_ObjectTreeList_DescribesAddAndRemove()
    {
        // Act
        var cut = RenderObjectTree(new List<int> { 1 });

        // Assert
        await Assert.That(cut.Find(".add").GetAttribute(TooltipAttribute)).IsEqualTo("Add element");
        await Assert.That(cut.Find(".remove").GetAttribute(TooltipAttribute)).IsEqualTo("Remove element");
    }
}
