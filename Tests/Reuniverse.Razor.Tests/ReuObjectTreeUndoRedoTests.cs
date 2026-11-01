namespace Reuniverse.Razor.Tests;

public class ReuObjectTreeUndoRedoTests : ReuObjectTreeTestBase
{
    private struct MutablePoint
    {
        public int X { get; set; }
    }

    private class Inner;

    private class Model
    {
        public string Name { get; set; } = "Ann";
        public bool Flag { get; set; }
        public string? Nickname { get; set; } = "nick";
        public Inner? Child { get; set; }
        public MutablePoint Point { get; set; }
        public DayOfWeek Day { get; set; } = DayOfWeek.Monday;
        public List<int> Numbers { get; set; } = [1, 2];
    }

    private ReuObjectTreeHistory activeHistory = default!;
    private IRenderedComponent<ReuObjectTree> activeTree = default!;

    public ReuObjectTreeUndoRedoTests()
    {
        AcceptConfirmDialogs();
    }

    private IRenderedComponent<ReuObjectTree> RenderWithHistory(object value, ReuObjectTreeHistory? history = null)
    {
        activeHistory = history ?? new ReuObjectTreeHistory();

        activeTree = Render<ReuObjectTree>(parameters => parameters
            .Add(x => x.Value, value)
            .Add(x => x.IsExpandedByDefault, true)
            .Add(x => x.History, activeHistory));

        return activeTree;
    }

    private Task<bool> UndoAsync() => activeTree.InvokeAsync(activeHistory.Undo);
    private Task<bool> RedoAsync() => activeTree.InvokeAsync(activeHistory.Redo);

    private static void EditText(IRenderedComponent<ReuObjectTree> cut, string property, string value)
    {
        cut.MemberRow(property).QuerySelector("input")!.Change(value);
    }

    [Test]
    public async Task Edit_WithHistory_RecordsStep()
    {
        // Arrange
        var history = new ReuObjectTreeHistory();
        var cut = Render<ReuObjectTree>(parameters => parameters
            .Add(x => x.Value, new Model())
            .Add(x => x.IsExpandedByDefault, true)
            .Add(x => x.History, history));

        // Act
        EditText(cut, nameof(Model.Name), "Bob");

        // Assert
        await Assert.That(history.UndoDescription).IsEqualTo("Set Name");
    }

    [Test]
    public async Task Edit_WithHistory_RaisesChanged()
    {
        // Arrange
        var history = new ReuObjectTreeHistory();
        var raised = false;
        history.Changed += () => raised = true;
        var cut = RenderWithHistory(new Model(), history);

        // Act
        EditText(cut, nameof(Model.Name), "Bob");

        // Assert
        await Assert.That(raised).IsTrue();
    }

    [Test]
    public async Task Edit_ToSameValue_RecordsNothing()
    {
        // Arrange
        var history = new ReuObjectTreeHistory();
        var cut = RenderWithHistory(new Model(), history);

        // Act
        EditText(cut, nameof(Model.Name), "Ann");

        // Assert
        await Assert.That(history.CanUndo).IsFalse();
    }

    [Test]
    public async Task Edit_WithMutableStructMember_RecordsNothing()
    {
        // Arrange
        var history = new ReuObjectTreeHistory();
        var cut = RenderWithHistory(new Model(), history);
        cut.MemberRow(nameof(Model.Point)).QuerySelector(".expander")!.Click();

        // Act
        EditText(cut, nameof(MutablePoint.X), "5");

        // Assert
        await Assert.That(history.CanUndo).IsFalse();
    }

    [Test]
    public async Task Undo_AfterPropertyEdit_RestoresValue()
    {
        // Arrange
        var model = new Model();
        var cut = RenderWithHistory(model);
        EditText(cut, nameof(Model.Name), "Bob");

        // Act
        await UndoAsync();

        // Assert
        await Assert.That(model.Name).IsEqualTo("Ann");
    }

    [Test]
    public async Task Undo_AfterPropertyEdit_ShowsRestoredValue()
    {
        // Arrange
        var cut = RenderWithHistory(new Model());
        EditText(cut, nameof(Model.Name), "Bob");

        // Act
        await UndoAsync();

        // Assert
        await Assert.That(cut.MemberRow(nameof(Model.Name)).QuerySelector("input")!.GetAttribute("value")).IsEqualTo("Ann");
    }

    [Test]
    public async Task Redo_AfterUndo_ReappliesValue()
    {
        // Arrange
        var model = new Model();
        var cut = RenderWithHistory(model);
        EditText(cut, nameof(Model.Name), "Bob");
        await UndoAsync();

        // Act
        await RedoAsync();

        // Assert
        await Assert.That(model.Name).IsEqualTo("Bob");
    }

    [Test]
    public async Task Undo_AfterSeveralEdits_RevertsInReverseOrder()
    {
        // Arrange
        var model = new Model();
        var cut = RenderWithHistory(model);
        EditText(cut, nameof(Model.Name), "Bob");
        EditText(cut, nameof(Model.Name), "Cid");

        // Act
        await UndoAsync();

        // Assert
        await Assert.That(model.Name).IsEqualTo("Bob");
    }

    [Test]
    public async Task Undo_AfterCheckboxToggle_RestoresValue()
    {
        // Arrange
        var model = new Model();
        var cut = RenderWithHistory(model);
        cut.MemberRow(nameof(Model.Flag)).QuerySelector("input")!.Change(true);

        // Act
        await UndoAsync();

        // Assert
        await Assert.That(model.Flag).IsFalse();
    }

    [Test]
    public async Task Undo_AfterEnumChange_RestoresValue()
    {
        // Arrange
        var model = new Model();
        var cut = RenderWithHistory(model);
        cut.MemberRow(nameof(Model.Day)).QuerySelector("select")!.Change("3");

        // Act
        await UndoAsync();

        // Assert
        await Assert.That(model.Day).IsEqualTo(DayOfWeek.Monday);
    }

    [Test]
    public async Task Undo_AfterCreateInstance_RestoresNull()
    {
        // Arrange
        var model = new Model();
        var cut = RenderWithHistory(model);
        cut.MemberRow(nameof(Model.Child)).QuerySelector(".box-value.clickable")!.Click();

        // Act
        await UndoAsync();

        // Assert
        await Assert.That(model.Child).IsNull();
    }

    [Test]
    public async Task Redo_AfterCreateInstance_RestoresSameInstance()
    {
        // Arrange
        var model = new Model();
        var cut = RenderWithHistory(model);
        cut.MemberRow(nameof(Model.Child)).QuerySelector(".box-value.clickable")!.Click();
        var created = model.Child;
        await UndoAsync();

        // Act
        await RedoAsync();

        // Assert
        await Assert.That(model.Child).IsSameReferenceAs(created);
    }

    [Test]
    public async Task Undo_AfterSetToNull_RestoresValue()
    {
        // Arrange
        var model = new Model();
        var cut = RenderWithHistory(model);
        cut.MemberRow(nameof(Model.Nickname)).QuerySelector(".remove")!.Click();

        // Act
        await UndoAsync();

        // Assert
        await Assert.That(model.Nickname).IsEqualTo("nick");
    }

    [Test]
    public async Task Undo_AfterElementEdit_RestoresElement()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        var cut = RenderWithHistory(list);
        cut.EditNumber(1, "20");

        // Act
        await UndoAsync();

        // Assert
        await list.ShouldEqualInOrderAsync(1, 2, 3);
    }

    [Test]
    public async Task Redo_AfterElementEdit_ReappliesElement()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        var cut = RenderWithHistory(list);
        cut.EditNumber(1, "20");
        await UndoAsync();

        // Act
        await RedoAsync();

        // Assert
        await list.ShouldEqualInOrderAsync(1, 20, 3);
    }

    [Test]
    public async Task Undo_AfterArrayElementEdit_RestoresElement()
    {
        // Arrange
        var array = new[] { 1, 2 };
        var cut = RenderWithHistory(array);
        cut.EditNumber(1, "20");

        // Act
        await UndoAsync();

        // Assert
        await array.ShouldEqualInOrderAsync(1, 2);
    }

    [Test]
    public async Task Undo_AfterElementRemoved_RestoresPosition()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        var cut = RenderWithHistory(list);
        cut.RemoveRow(1);

        // Act
        await UndoAsync();

        // Assert
        await list.ShouldEqualInOrderAsync(1, 2, 3);
    }

    [Test]
    public async Task Redo_AfterElementRemoved_RemovesElementAgain()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        var cut = RenderWithHistory(list);
        cut.RemoveRow(1);
        await UndoAsync();

        // Act
        await RedoAsync();

        // Assert
        await list.ShouldEqualInOrderAsync(1, 3);
    }

    [Test]
    public async Task Undo_AfterElementAdded_RemovesElement()
    {
        // Arrange
        var list = new List<int> { 1 };
        var cut = RenderWithHistory(list);
        cut.Find(".add").Click();

        // Act
        await UndoAsync();

        // Assert
        await list.ShouldEqualInOrderAsync(1);
    }

    [Test]
    public async Task Redo_AfterElementAdded_AddsSameInstance()
    {
        // Arrange
        var list = new List<Inner>();
        var cut = RenderWithHistory(list);
        cut.Find(".add").Click();
        var added = list[0];
        await UndoAsync();

        // Act
        await RedoAsync();

        // Assert
        await Assert.That(list[0]).IsSameReferenceAs(added);
    }

    [Test]
    public async Task Undo_AfterKeyAdded_RemovesKey()
    {
        // Arrange
        var dictionary = new Dictionary<string, int>();
        var cut = RenderWithHistory(dictionary);
        cut.Find("input").Change("key");
        cut.Find(".add").Click();

        // Act
        await UndoAsync();

        // Assert
        await Assert.That(dictionary.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Redo_AfterKeyAdded_AddsKeyAgain()
    {
        // Arrange
        var dictionary = new Dictionary<string, int>();
        var cut = RenderWithHistory(dictionary);
        cut.Find("input").Change("key");
        cut.Find(".add").Click();
        await UndoAsync();

        // Act
        await RedoAsync();

        // Assert
        await dictionary.Keys.ShouldEqualInOrderAsync("key");
    }

    [Test]
    public async Task Undo_AfterKeyRemoved_RestoresKeyAndValue()
    {
        // Arrange
        var dictionary = new Dictionary<string, int> { ["a"] = 5 };
        var cut = RenderWithHistory(dictionary);
        cut.RemoveRow(0);

        // Act
        await UndoAsync();

        // Assert
        await Assert.That(dictionary["a"]).IsEqualTo(5);
    }

    [Test]
    public async Task Redo_AfterKeyRemoved_RemovesKeyAgain()
    {
        // Arrange
        var dictionary = new Dictionary<string, int> { ["a"] = 5 };
        var cut = RenderWithHistory(dictionary);
        cut.RemoveRow(0);
        await UndoAsync();

        // Act
        await RedoAsync();

        // Assert
        await Assert.That(dictionary.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Undo_WithNestedEdit_RestoresNestedValue()
    {
        // Arrange
        var model = new Model();
        var cut = RenderWithHistory(model);
        cut.MemberRow(nameof(Model.Numbers)).QuerySelector(".expander")!.Click();
        cut.EditNumber(0, "9");

        // Act
        await UndoAsync();

        // Assert
        await model.Numbers.ShouldEqualInOrderAsync(1, 2);
    }

    [Test]
    public async Task Undo_AfterListChangedElsewhere_LeavesListAlone()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        var cut = RenderWithHistory(list);
        cut.RemoveRow(2);
        list.Clear();

        // Act
        await UndoAsync();

        // Assert
        await Assert.That(list.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Undo_AfterListChangedElsewhere_ReturnsFalse()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        var cut = RenderWithHistory(list);
        cut.RemoveRow(2);
        list.Clear();

        // Act
        var succeeded = await UndoAsync();

        // Assert
        await Assert.That(succeeded).IsFalse();
    }

    [Test]
    public async Task Undo_AfterElementEditAndListChangedElsewhere_DoesNotOverwrite()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        var cut = RenderWithHistory(list);
        cut.EditNumber(1, "20");
        list.Insert(0, 0); // index 1 now holds 1, not the edited element

        // Act
        await UndoAsync();

        // Assert
        await list.ShouldEqualInOrderAsync(0, 1, 20, 3);
    }

    [Test]
    public async Task Undo_WithEditInsideDictionaryValue_RestoresElement()
    {
        // Arrange
        var numbers = new List<int> { 1 };
        var cut = RenderWithHistory(new Dictionary<string, List<int>> { ["a"] = numbers });
        cut.ExpandFirstMember();
        cut.EditNumber(0, "9");

        // Act
        await UndoAsync();

        // Assert
        await numbers.ShouldEqualInOrderAsync(1);
    }

    [Test]
    public async Task Undo_WithElementAddedInsideDictionaryValue_RemovesElement()
    {
        // Arrange
        var numbers = new List<int> { 1 };
        var cut = RenderWithHistory(new Dictionary<string, List<int>> { ["a"] = numbers });
        cut.FindAll(".add")[1].Click(); // the first one belongs to the dictionary

        // Act
        await UndoAsync();

        // Assert
        await numbers.ShouldEqualInOrderAsync(1);
    }

    [Test]
    public async Task Undo_WithEditInsideListElement_RestoresElement()
    {
        // Arrange
        var numbers = new List<int> { 1 };
        var cut = RenderWithHistory(new List<List<int>> { numbers });
        cut.ExpandFirstMember();
        cut.EditNumber(0, "9");

        // Act
        await UndoAsync();

        // Assert
        await numbers.ShouldEqualInOrderAsync(1);
    }

    [Test]
    public async Task Undo_WithElementAddedInsideListElement_RemovesElement()
    {
        // Arrange
        var numbers = new List<int> { 1 };
        var cut = RenderWithHistory(new List<List<int>> { numbers });
        cut.FindAll(".add")[1].Click(); // the first one belongs to the outer list

        // Act
        await UndoAsync();

        // Assert
        await numbers.ShouldEqualInOrderAsync(1);
    }

    [Test]
    public async Task Undo_WithElementAddedInsideMember_RemovesElement()
    {
        // Arrange
        var model = new Model();
        var cut = RenderWithHistory(model);
        cut.MemberRow(nameof(Model.Numbers)).QuerySelector(".add")!.Click();

        // Act
        await UndoAsync();

        // Assert
        await model.Numbers.ShouldEqualInOrderAsync(1, 2);
    }

    [Test]
    public async Task Undo_AfterElementAddedAndReplacedElsewhere_KeepsElement()
    {
        // Arrange
        var list = new List<int> { 1 };
        var cut = RenderWithHistory(list);
        cut.Find(".add").Click();
        list[1] = 5; // the added element is gone, so index 1 must not be removed

        // Act
        await UndoAsync();

        // Assert
        await list.ShouldEqualInOrderAsync(1, 5);
    }

    [Test]
    public async Task Redo_AfterElementReplacedElsewhere_KeepsElement()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        var cut = RenderWithHistory(list);
        cut.RemoveRow(1);
        await UndoAsync();
        list[1] = 9; // index 1 no longer holds the removed element

        // Act
        await RedoAsync();

        // Assert
        await list.ShouldEqualInOrderAsync(1, 9, 3);
    }

    [Test]
    public async Task Undo_FromSharedHistory_UpdatesOtherTree()
    {
        // Arrange
        var history = new ReuObjectTreeHistory();
        var model = new Model();
        var first = RenderWithHistory(model, history);
        var second = RenderWithHistory(model, history);
        EditText(first, nameof(Model.Name), "Bob");

        // Act
        await second.InvokeAsync(history.Undo);

        // Assert
        await Assert.That(second.MemberRow(nameof(Model.Name)).QuerySelector("input")!.GetAttribute("value")).IsEqualTo("Ann");
    }

    [Test]
    public async Task Undo_FromOutsideTree_UpdatesTree()
    {
        // Arrange
        var history = new ReuObjectTreeHistory();
        var cut = Render<ReuObjectTree>(parameters => parameters
            .Add(x => x.Value, new Model())
            .Add(x => x.IsExpandedByDefault, true)
            .Add(x => x.History, history));
        EditText(cut, nameof(Model.Name), "Bob");

        // Act
        await cut.InvokeAsync(history.Undo);

        // Assert
        await Assert.That(cut.MemberRow(nameof(Model.Name)).QuerySelector("input")!.GetAttribute("value")).IsEqualTo("Ann");
    }

    [Test]
    public async Task Dispose_WithHistory_UnsubscribesFromHistory()
    {
        // Arrange
        var history = new ReuObjectTreeHistory();
        var cut = RenderWithHistory(new Model(), history);
        EditText(cut, nameof(Model.Name), "Bob");
        await DisposeComponentsAsync();

        // Act
        var act = () => history.Undo();

        // Assert
        await Assert.That(act).ThrowsNothing();
    }
}
