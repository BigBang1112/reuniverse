namespace Reuniverse.Razor.Tests;

public class ReuObjectTreeItemTests
{
    [Test]
    public async Task SetValue_WithNull_ResetsValueTypeElement()
    {
        // Arrange
        var list = new List<int> { 5 };
        var item = new ReuObjectTreeItem(list, 0, 5);

        // Act
        var changed = item.SetValue(null);

        // Assert
        await Assert.That(changed).IsTrue();
        await list.ShouldEqualInOrderAsync(0);
    }

    [Test]
    public async Task SetValue_WithNonListParent_Throws()
    {
        // Arrange
        var item = new ReuObjectTreeItem(new HashSet<int> { 1 }, 0, 1);

        // Act
        var act = () => item.SetValue(2);

        // Assert
        await Assert.That(act).Throws<InvalidOperationException>();
    }

    [Test]
    [Arguments(5, "#3 5")]
    [Arguments(null, "#3 null")]
    public async Task ToString_WithValue_ShowsIndexAndValue(int? value, string expected)
    {
        // Arrange
        var item = new ReuObjectTreeItem(new List<int?>(), 3, value);

        // Act
        var text = item.ToString();

        // Assert
        await Assert.That(text).IsEqualTo(expected);
    }

    [Test]
    public async Task SetValue_WithSameValue_ReturnsFalse()
    {
        // Arrange
        var list = new List<int> { 5 };
        var item = new ReuObjectTreeItem(list, 0, 5);

        // Act
        var changed = item.SetValue(5);

        // Assert
        await Assert.That(changed).IsFalse();
    }

    [Test]
    public async Task SetValue_WithNullElement_ReplacesElement()
    {
        // Arrange
        var list = new List<string?> { null };
        var item = new ReuObjectTreeItem(list, 0, null);

        // Act
        var changed = item.SetValue("a");

        // Assert
        await Assert.That(changed).IsTrue();
        await list.ShouldEqualInOrderAsync("a");
    }

    [Test]
    public async Task SetValue_WithDifferentElementAtIndex_ThrowsWithoutWriting()
    {
        // Arrange
        var list = new List<int> { 1 };
        var item = new ReuObjectTreeItem(list, 0, 2);

        // Act
        var act = () => item.SetValue(3);

        // Assert
        await Assert.That(act).Throws<InvalidOperationException>();
        await list.ShouldEqualInOrderAsync(1);
    }

    [Test]
    public async Task SetValue_WithIndexPastTheEnd_ThrowsWithoutWriting()
    {
        // Arrange
        var list = new List<int> { 1 };
        var item = new ReuObjectTreeItem(list, 5, 1);

        // Act
        var act = () => item.SetValue(3);

        // Assert
        await Assert.That(act).Throws<InvalidOperationException>();
        await list.ShouldEqualInOrderAsync(1);
    }
}
