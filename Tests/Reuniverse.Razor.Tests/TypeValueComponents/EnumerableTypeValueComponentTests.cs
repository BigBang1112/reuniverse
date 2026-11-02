using System.Collections;

namespace Reuniverse.Razor.Tests.TypeValueComponents;

public class EnumerableTypeValueComponentTests : ReuObjectTreeTestBase
{
    private abstract class AbstractElement;

    private sealed class NoParameterlessConstructor(int value)
    {
        public int Value { get; } = value;
    }

    private sealed class Creatable;

    private static int AddButtonCount(IRenderedComponent<ReuObjectTree> cut) => cut.FindAll(".add").Count;

    [Test]
    [Arguments(1, "1 element")]
    [Arguments(2, "2 elements")]
    public async Task Render_WithList_ShowsElementCount(int count, string expected)
    {
        // Act
        var cut = RenderObjectTree(Enumerable.Range(0, count).ToList());

        // Assert
        await Assert.That(cut.Find(".box-value").TextContent).IsEqualTo(expected);
    }

    [Test]
    public async Task AddElement_WithStrings_AddsEmptyString()
    {
        // Arrange
        var list = new List<string>();
        var cut = RenderObjectTree(list);

        // Act
        cut.Find(".add").Click();

        // Assert
        await list.ShouldEqualInOrderAsync("");
    }

    [Test]
    public async Task AddElement_WithValueTypes_AddsDefault()
    {
        // Arrange
        var list = new List<int>();
        var cut = RenderObjectTree(list);

        // Act
        cut.Find(".add").Click();

        // Assert
        await list.ShouldEqualInOrderAsync(0);
    }

    [Test]
    public async Task AddElement_WithCreatableType_AddsNewInstance()
    {
        // Arrange
        var list = new List<Creatable>();
        var cut = RenderObjectTree(list);

        // Act
        cut.Find(".add").Click();

        // Assert
        await Assert.That(list.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Render_WithAbstractElementType_HidesAddButton()
    {
        // Act
        var cut = RenderObjectTree(new List<AbstractElement>());

        // Assert
        await Assert.That(AddButtonCount(cut)).IsEqualTo(0);
    }

    [Test]
    public async Task Render_WithoutParameterlessConstructor_HidesAddButton()
    {
        // Act
        var cut = RenderObjectTree(new List<NoParameterlessConstructor>());

        // Assert
        await Assert.That(AddButtonCount(cut)).IsEqualTo(0);
    }

    [Test]
    public async Task Render_WithArray_HidesAddButton()
    {
        // Act
        var cut = RenderObjectTree(new[] { 1, 2 });

        // Assert
        await Assert.That(AddButtonCount(cut)).IsEqualTo(0);
    }

    [Test]
    public async Task Render_WithNonGenericList_HidesAddButton()
    {
        // Act
        var cut = RenderObjectTree(new ArrayList());

        // Assert
        await Assert.That(AddButtonCount(cut)).IsEqualTo(0);
    }
}
