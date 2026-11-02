using Microsoft.AspNetCore.Components;

namespace Reuniverse.Razor.Tests.TypeComponents;

public class ReuObjectTreeMemberTypeComponentTests : ReuObjectTreeTestBase
{
    private class Inner
    {
        public int Value { get; set; } = 1;
    }

    private abstract class AbstractInner;

    private sealed class ConcreteInner : AbstractInner;

    private class Model
    {
        public string Required { get; set; } = "required";
        public string? Nickname { get; set; } = "nick";
        public int? Score { get; set; } = 5;
        public int Age { get; set; } = 30;
        public Inner? Child { get; set; }
        public AbstractInner? Abstract { get; set; }
        public object Boxed { get; set; } = "text";
        public AbstractInner Derived { get; set; } = new ConcreteInner();
        public string WriteOnly { set { } }
        public int Throws => throw new InvalidOperationException("getter failed");
    }

    private IRenderedComponent<ReuObjectTree> RenderWithProviders(Model model, Func<object?, RenderFragment?>? visualization = null, Func<object?, RenderFragment?>? extras = null)
    {
        return Render<ReuObjectTree>(parameters => parameters
            .Add(x => x.Value, model)
            .Add(x => x.IsExpandedByDefault, true)
            .Add(x => x.VisualizationProvider, visualization)
            .Add(x => x.ExtrasProvider, extras));
    }

    private static RenderFragment? ProvideFor(object? value, string name, string text)
    {
        return value is ReuObjectTreeMember member && member.Property.Name == name
            ? builder => builder.AddContent(0, text)
            : null;
    }

    [Test]
    public async Task Render_WithNullableReferenceProperty_MarksTypeAsNullable()
    {
        // Act
        var cut = RenderObjectTree(new Model());

        // Assert
        await Assert.That(cut.MemberRow("Nickname").QuerySelector(".box.type")!.TextContent).Contains("?");
    }

    [Test]
    public async Task Render_WithNonNullableReferenceProperty_DoesNotMarkType()
    {
        // Act
        var cut = RenderObjectTree(new Model());

        // Assert
        await Assert.That(cut.MemberRow("Required").QuerySelector(".box.type")!.TextContent).DoesNotContain("?");
    }

    [Test]
    public async Task Render_WithThrowingGetter_ShowsError()
    {
        // Act
        var cut = RenderObjectTree(new Model());

        // Assert
        await Assert.That(cut.MemberRow("Throws").QuerySelector(".box-value.error")!.TextContent).IsEqualTo("getter failed");
    }

    [Test]
    public async Task Render_WithWriteOnlyProperty_ShowsCannotGet()
    {
        // Act
        var cut = RenderObjectTree(new Model());

        // Assert
        await Assert.That(cut.MemberRow("WriteOnly").TextContent).Contains("(cannot get)");
    }

    [Test]
    public async Task Render_WithDifferentRuntimeType_ShowsRuntimeType()
    {
        // Act
        var cut = RenderObjectTree(new Model());

        // Assert
        await Assert.That(cut.MemberRow("Derived").QuerySelector(".box-value.type")!.TextContent).Contains("ConcreteInner");
    }

    [Test]
    public async Task CreateInstance_WithNullProperty_AssignsNewInstance()
    {
        // Arrange
        var model = new Model();
        var cut = RenderObjectTree(model);

        // Act
        cut.MemberRow("Child").QuerySelector(".box-value.clickable")!.Click();

        // Assert
        await Assert.That(model.Child).IsNotNull();
    }

    [Test]
    public async Task Render_WithNullAbstractProperty_OffersNoInstance()
    {
        // Act
        var cut = RenderObjectTree(new Model());

        // Assert
        await Assert.That(cut.MemberRow("Abstract").QuerySelectorAll(".box-value.clickable").Length).IsEqualTo(0);
    }

    [Test]
    public async Task RemoveValue_WhenConfirmed_SetsNull()
    {
        // Arrange
        AcceptConfirmDialogs();
        var model = new Model();
        var cut = RenderObjectTree(model);

        // Act
        cut.MemberRow("Nickname").QuerySelector(".remove")!.Click();

        // Assert
        await Assert.That(model.Nickname).IsNull();
    }

    [Test]
    public async Task RemoveValue_WhenDeclined_KeepsValue()
    {
        // Arrange
        JSInterop.Setup<bool>("confirm", _ => true).SetResult(false);
        var model = new Model();
        var cut = RenderObjectTree(model);

        // Act
        cut.MemberRow("Nickname").QuerySelector(".remove")!.Click();

        // Assert
        await Assert.That(model.Nickname).IsEqualTo("nick");
    }

    [Test]
    [Arguments("Required")]
    [Arguments("Age")]
    public async Task Render_WithNonNullableProperty_HidesRemoveButton(string property)
    {
        // Act
        var cut = RenderObjectTree(new Model());

        // Assert
        await Assert.That(cut.MemberRow(property).QuerySelectorAll(".remove").Length).IsEqualTo(0);
    }

    [Test]
    public async Task Render_WithNullableValueProperty_ShowsRemoveButton()
    {
        // Act
        var cut = RenderObjectTree(new Model());

        // Assert
        await Assert.That(cut.MemberRow("Score").QuerySelectorAll(".remove").Length).IsEqualTo(1);
    }

    [Test]
    public async Task RevertValue_AfterRemove_RestoresValue()
    {
        // Arrange
        AcceptConfirmDialogs();
        var model = new Model();
        var cut = RenderObjectTree(model);
        cut.MemberRow("Nickname").QuerySelector(".remove")!.Click();

        // Act
        cut.MemberRow("Nickname").QuerySelectorAll(".box-value.clickable").First(x => x.TextContent == "Revert").Click();

        // Assert
        await Assert.That(model.Nickname).IsEqualTo("nick");
    }

    [Test]
    public async Task Render_WithVisualizationProvider_ShowsVisualization()
    {
        // Act
        var cut = RenderWithProviders(new Model(), value => ProvideFor(value, "Age", "visualized"));

        // Assert
        await Assert.That(cut.MemberRow("Age").TextContent).Contains("visualized");
    }

    [Test]
    public async Task Reveal_WithVisualization_ShowsActualValue()
    {
        // Arrange
        var cut = RenderWithProviders(new Model(), value => ProvideFor(value, "Age", "visualized"));

        // Act
        cut.MemberRow("Age").QuerySelector(".box-value.clickable")!.Click();

        // Assert
        await Assert.That(cut.MemberRow("Age").QuerySelector("input")!.GetAttribute("value")).IsEqualTo("30");
    }

    [Test]
    public async Task Visualize_AfterReveal_ShowsVisualizationAgain()
    {
        // Arrange
        var cut = RenderWithProviders(new Model(), value => ProvideFor(value, "Age", "visualized"));
        cut.MemberRow("Age").QuerySelector(".box-value.clickable")!.Click();

        // Act
        cut.MemberRow("Age").QuerySelectorAll(".box-value.clickable").First(x => x.TextContent == "Visualize").Click();

        // Assert
        await Assert.That(cut.MemberRow("Age").TextContent).Contains("visualized");
    }

    [Test]
    public async Task Render_WithExtrasProvider_ShowsExtras()
    {
        // Act
        var cut = RenderWithProviders(new Model(), extras: value => ProvideFor(value, "Age", "extra content"));

        // Assert
        await Assert.That(cut.MemberRow("Age").TextContent).Contains("extra content");
    }
}
