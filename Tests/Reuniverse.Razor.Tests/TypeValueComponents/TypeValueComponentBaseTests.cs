namespace Reuniverse.Razor.Tests.TypeValueComponents;

public class TypeValueComponentBaseTests : ReuObjectTreeTestBase
{
    private const string StaleEditMessage = "The list has changed";

    private struct MutablePoint
    {
        public int X { get; set; }
    }

    private readonly struct ReadOnlyPoint
    {
        public int X { get; init; }
    }

    private class Model
    {
        private int guarded;

        public int Guarded
        {
            get => guarded;
            set => guarded = value < 0 ? throw new ArgumentException("setter failed") : value;
        }

        public MutablePoint Mutable { get; set; }
        public ReadOnlyPoint ReadOnly { get; set; }
        public int Init { get; init; } = 1;
    }

    [Test]
    public async Task SetValue_WithThrowingSetter_ShowsInnerError()
    {
        // Arrange
        var cut = RenderObjectTree(new Model());

        // Act
        cut.MemberRow(nameof(Model.Guarded)).QuerySelector("input")!.Change("-1");

        // Assert
        await Assert.That(cut.Markup).Contains("setter failed");
    }

    [Test]
    public async Task SetValue_AfterThrowingSetter_ClearsError()
    {
        // Arrange
        var cut = RenderObjectTree(new Model());
        cut.MemberRow(nameof(Model.Guarded)).QuerySelector("input")!.Change("-1");

        // Act
        cut.MemberRow(nameof(Model.Guarded)).QuerySelector("input")!.Change("1");

        // Assert
        await Assert.That(cut.Markup).DoesNotContain("setter failed");
    }

    [Test]
    public async Task SetValue_WithInitProperty_UpdatesProperty()
    {
        // Arrange
        var model = new Model();
        var cut = RenderObjectTree(model);

        // Act
        cut.MemberRow(nameof(Model.Init)).QuerySelector("input")!.Change("2");

        // Assert
        await Assert.That(model.Init).IsEqualTo(2);
    }

    [Test]
    public async Task Render_WithReadOnlyStructMember_MakesInputReadOnly()
    {
        // Arrange
        var cut = RenderObjectTree(new Model());

        // Act
        cut.MemberRow(nameof(Model.ReadOnly)).QuerySelector(".expander")!.Click();

        // Assert
        await Assert.That(cut.MemberRow("X").QuerySelector("input")!.HasAttribute("readonly")).IsTrue();
    }

    [Test]
    [Skip("Known bug: the setter writes to a boxed copy of the struct, so the edit is lost")]
    public async Task SetValue_WithMutableStructMember_UpdatesParent()
    {
        // Arrange
        var model = new Model();
        var cut = RenderObjectTree(model);
        cut.MemberRow(nameof(Model.Mutable)).QuerySelector(".expander")!.Click();

        // Act
        cut.MemberRow("X").QuerySelector("input")!.Change("5");

        // Assert
        await Assert.That(model.Mutable.X).IsEqualTo(5);
    }

    [Test]
    public async Task SetValue_WithStaleRow_DoesNotOverwrite()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        var cut = RenderObjectTree(list);
        list.Insert(0, 0); // row #1 still shows 2, but index 1 now holds 1

        // Act
        cut.EditNumber(1, "99");

        // Assert
        await list.ShouldEqualInOrderAsync(0, 1, 2, 3);
    }

    [Test]
    public async Task SetValue_WithStaleRow_ShowsError()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        var cut = RenderObjectTree(list);
        list.Insert(0, 0);

        // Act
        cut.EditNumber(1, "99");

        // Assert
        await Assert.That(cut.Markup).Contains(StaleEditMessage);
    }

    [Test]
    public async Task SetValue_WithStaleRow_RefreshesRows()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        var cut = RenderObjectTree(list);
        list.Insert(0, 0);

        // Act
        cut.EditNumber(1, "99");

        // Assert
        await Assert.That(cut.NumberInputCount()).IsEqualTo(4);
        await Assert.That(cut.NumberValue(1)).IsEqualTo("1");
    }

    [Test]
    public async Task SetValue_AfterStaleEdit_AppliesEdit()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        var cut = RenderObjectTree(list);
        list.Insert(0, 0);
        cut.EditNumber(1, "99");

        // Act
        cut.EditNumber(1, "50");

        // Assert
        await list.ShouldEqualInOrderAsync(0, 50, 2, 3);
    }

    [Test]
    public async Task SetValue_AfterStaleEdit_ClearsError()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        var cut = RenderObjectTree(list);
        list.Insert(0, 0);
        cut.EditNumber(1, "99");

        // Act
        cut.EditNumber(1, "50");

        // Assert
        await Assert.That(cut.Markup).DoesNotContain(StaleEditMessage);
    }

    [Test]
    public async Task SetValue_WithRowPastTheEnd_DoesNotWrite()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        var cut = RenderObjectTree(list);
        list.RemoveRange(1, 2);

        // Act
        cut.EditNumber(2, "99");

        // Assert
        await list.ShouldEqualInOrderAsync(1);
    }

    [Test]
    public async Task SetValue_WithRowPastTheEnd_RemovesRow()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        var cut = RenderObjectTree(list);
        list.RemoveRange(1, 2);

        // Act
        cut.EditNumber(2, "99");

        // Assert
        await Assert.That(cut.NumberInputCount()).IsEqualTo(1);
    }

    [Test]
    public async Task SetValue_RepeatedlyOnCurrentRow_KeepsLastValue()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        var cut = RenderObjectTree(list);

        // Act
        cut.EditNumber(1, "20");
        cut.EditNumber(1, "30");

        // Assert
        await list.ShouldEqualInOrderAsync(1, 30, 3);
    }

    [Test]
    public async Task SetValue_WithArrayElement_UpdatesElement()
    {
        // Arrange
        var array = new[] { 1, 2 };
        var cut = RenderObjectTree(array);

        // Act
        cut.EditNumber(1, "9");

        // Assert
        await array.ShouldEqualInOrderAsync(1, 9);
    }

    [Test]
    public async Task SetValue_WithNaNElement_UpdatesElement()
    {
        // Arrange
        var list = new List<double> { double.NaN };
        var cut = RenderObjectTree(list);

        // Act
        cut.EditNumber(0, "1.5");

        // Assert
        await list.ShouldEqualInOrderAsync(1.5);
    }
}
