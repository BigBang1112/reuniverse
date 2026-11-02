using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Reuniverse.Razor.Tests.TypeComponents;

public class ReuObjectTreeItemTypeComponentTests : ReuObjectTreeTestBase
{
    public ReuObjectTreeItemTypeComponentTests()
    {
        AcceptConfirmDialogs();
    }

    private class ThrowsOnToString
    {
        public override string ToString() => throw new InvalidOperationException("ToString failed");
    }

    private static object CreateList(string type) => type switch
    {
        "ReadOnlyCollection" => new ReadOnlyCollection<int>([1, 2]),
        "ImmutableList" => ImmutableList.Create(1, 2),
        "ImmutableArray" => ImmutableArray.Create(1, 2),
        "Array" => new[] { 1, 2 },
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    [Test]
    [Arguments("ReadOnlyCollection")]
    [Arguments("ImmutableList")]
    [Arguments("ImmutableArray")]
    [Arguments("Array")]
    public async Task Render_WithReadOnlyList_HidesRemoveButton(string type)
    {
        // Act
        var cut = RenderObjectTree(CreateList(type));

        // Assert
        await Assert.That(cut.RemoveButtonCount()).IsEqualTo(0);
    }

    [Test]
    public async Task Render_WithThrowingToString_ShowsError()
    {
        // Act
        var cut = RenderObjectTree(new List<ThrowsOnToString> { new() });

        // Assert
        await Assert.That(cut.Markup).Contains("ToString failed");
    }

    [Test]
    public async Task Render_WithNullElement_ShowsNull()
    {
        // Act
        var cut = RenderObjectTree(new List<string?> { null });

        // Assert
        await Assert.That(cut.Markup).Contains("null");
    }

    [Test]
    public async Task RemoveValue_WhenDeclined_KeepsElement()
    {
        // Arrange
        JSInterop.Setup<bool>("confirm", _ => true).SetResult(false);
        var list = new List<int> { 1, 2 };
        var cut = RenderObjectTree(list);

        // Act
        cut.RemoveRow(1);

        // Assert
        await list.ShouldEqualInOrderAsync(1, 2);
    }

    [Test]
    public async Task Render_WithMutableList_ShowsRemoveButtons()
    {
        // Act
        var cut = RenderObjectTree(new List<int> { 1, 2 });

        // Assert
        await Assert.That(cut.RemoveButtonCount()).IsEqualTo(2);
    }

    [Test]
    public async Task RemoveValue_WithCurrentRow_RemovesElement()
    {
        // Arrange
        var list = new List<string> { "a", "b", "c" };
        var cut = RenderObjectTree(list);

        // Act
        cut.RemoveRow(1);

        // Assert
        await list.ShouldEqualInOrderAsync("a", "c");
    }

    [Test]
    public async Task RemoveValue_WithNullElement_RemovesElement()
    {
        // Arrange
        var list = new List<string?> { "a", null };
        var cut = RenderObjectTree(list);

        // Act
        cut.RemoveRow(1);

        // Assert
        await list.ShouldEqualInOrderAsync("a");
    }

    [Test]
    public async Task RemoveValue_WithStaleRow_RemovesNothing()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        var cut = RenderObjectTree(list);
        list.Insert(0, 0); // row #1 still shows 2, but index 1 now holds 1

        // Act
        cut.RemoveRow(1);

        // Assert
        await list.ShouldEqualInOrderAsync(0, 1, 2, 3);
    }

    [Test]
    public async Task RemoveValue_WithStaleRow_RefreshesRows()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        var cut = RenderObjectTree(list);
        list.Insert(0, 0);

        // Act
        cut.RemoveRow(1);

        // Assert
        await Assert.That(cut.RemoveButtonCount()).IsEqualTo(4);
    }

    [Test]
    public async Task RemoveValue_WithRowPastTheEnd_RemovesNothing()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        var cut = RenderObjectTree(list);
        list.RemoveRange(1, 2);

        // Act
        cut.RemoveRow(2);

        // Assert
        await list.ShouldEqualInOrderAsync(1);
    }
}
