using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Reuniverse.Razor.Tests.TypeComponents;

public class ReuObjectTreeKeyValueTypeComponentTests : ReuObjectTreeTestBase
{
    private static object CreateDictionary(string type) => type switch
    {
        "ReadOnlyDictionary" => new ReadOnlyDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 }),
        "ImmutableDictionary" => ImmutableDictionary.Create<string, int>().Add("a", 1),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    [Test]
    [Arguments("ReadOnlyDictionary")]
    [Arguments("ImmutableDictionary")]
    public async Task Render_WithReadOnlyDictionary_HidesRemoveButton(string type)
    {
        // Act
        var cut = RenderObjectTree(CreateDictionary(type));

        // Assert
        await Assert.That(cut.RemoveButtonCount()).IsEqualTo(0);
    }

    [Test]
    public async Task RemoveValue_WhenConfirmed_RemovesKey()
    {
        // Arrange
        AcceptConfirmDialogs();
        var dictionary = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
        var cut = RenderObjectTree(dictionary);

        // Act
        cut.RemoveRow(0);

        // Assert
        await dictionary.Keys.ShouldEqualInOrderAsync("b");
    }

    [Test]
    public async Task RemoveValue_WhenDeclined_KeepsKey()
    {
        // Arrange
        JSInterop.Setup<bool>("confirm", _ => true).SetResult(false);
        var dictionary = new Dictionary<string, int> { ["a"] = 1 };
        var cut = RenderObjectTree(dictionary);

        // Act
        cut.RemoveRow(0);

        // Assert
        await dictionary.Keys.ShouldEqualInOrderAsync("a");
    }

    [Test]
    public async Task Render_WithNullValue_ShowsNull()
    {
        // Act
        var cut = RenderObjectTree(new Dictionary<string, object?> { ["a"] = null });

        // Assert
        await Assert.That(cut.Markup).Contains(">null<");
    }

    [Test]
    public async Task Render_WithValueOfOtherType_ShowsValueType()
    {
        // Act
        var cut = RenderObjectTree(new Dictionary<string, object?> { ["a"] = 5 });

        // Assert
        await Assert.That(cut.Find(".box-value.type").TextContent).Contains("int");
    }

    [Test]
    public async Task Render_WithMutableDictionary_ShowsRemoveButton()
    {
        // Act
        var cut = RenderObjectTree(new Dictionary<string, int> { ["a"] = 1 });

        // Assert
        await Assert.That(cut.RemoveButtonCount()).IsEqualTo(1);
    }
}
