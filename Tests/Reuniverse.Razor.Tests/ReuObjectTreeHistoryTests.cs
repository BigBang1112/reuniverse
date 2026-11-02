namespace Reuniverse.Razor.Tests;

public class ReuObjectTreeHistoryTests
{
    private int value;

    private ReuObjectTreeHistory CreateHistoryWithEdit(int capacity = 100)
    {
        var history = new ReuObjectTreeHistory(capacity);
        RecordEdit(history, 1);
        return history;
    }

    private void RecordEdit(ReuObjectTreeHistory history, int newValue)
    {
        var oldValue = value;
        value = newValue;

        history.Record($"Set {newValue}", () => { value = oldValue; return true; }, () => { value = newValue; return true; });
    }

    private static void RecordFailingEdit(ReuObjectTreeHistory history)
    {
        history.Record("Failing", () => false, () => false);
    }

    [Test]
    public async Task Constructor_ByDefault_HasNothingToUndoOrRedo()
    {
        // Act
        var history = new ReuObjectTreeHistory();

        // Assert
        await Assert.That(history.CanUndo).IsFalse();
        await Assert.That(history.CanRedo).IsFalse();
    }

    [Test]
    [Arguments(0)]
    [Arguments(-5)]
    public async Task Constructor_WithCapacityBelowOne_UsesOne(int capacity)
    {
        // Act
        var history = new ReuObjectTreeHistory(capacity);

        // Assert
        await Assert.That(history.Capacity).IsEqualTo(1);
    }

    [Test]
    public async Task Record_WithNullUndo_Throws()
    {
        // Arrange
        var history = new ReuObjectTreeHistory();

        // Act
        var act = () => history.Record("Edit", null!, () => true);

        // Assert
        await Assert.That(act).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Record_WithEdit_RaisesChanged()
    {
        // Arrange
        var history = new ReuObjectTreeHistory();
        var raised = false;
        history.Changed += () => raised = true;

        // Act
        RecordEdit(history, 1);

        // Assert
        await Assert.That(raised).IsTrue();
    }

    [Test]
    public async Task Record_WithEdit_DoesNotRaiseApplied()
    {
        // Arrange
        var history = new ReuObjectTreeHistory();
        var raised = false;
        history.Applied += () => raised = true;

        // Act
        RecordEdit(history, 1);

        // Assert
        await Assert.That(raised).IsFalse();
    }

    [Test]
    public async Task Record_AfterUndo_DiscardsRedoSteps()
    {
        // Arrange
        var history = CreateHistoryWithEdit();
        history.Undo();

        // Act
        RecordEdit(history, 2);

        // Assert
        await Assert.That(history.CanRedo).IsFalse();
    }

    [Test]
    public async Task Record_BeyondCapacity_DiscardsOldestSteps()
    {
        // Arrange
        var history = CreateHistoryWithEdit(capacity: 1);
        RecordEdit(history, 2);

        // Act
        history.Undo();

        // Assert
        await Assert.That(value).IsEqualTo(1);
        await Assert.That(history.CanUndo).IsFalse();
    }

    [Test]
    public async Task Undo_WithoutSteps_ReturnsFalse()
    {
        // Arrange
        var history = new ReuObjectTreeHistory();

        // Act
        var succeeded = history.Undo();

        // Assert
        await Assert.That(succeeded).IsFalse();
    }

    [Test]
    public async Task Undo_WithEdit_RevertsEdit()
    {
        // Arrange
        var history = CreateHistoryWithEdit();

        // Act
        var succeeded = history.Undo();

        // Assert
        await Assert.That(succeeded).IsTrue();
        await Assert.That(value).IsEqualTo(0);
    }

    [Test]
    public async Task Undo_WithSeveralEdits_RevertsLatestFirst()
    {
        // Arrange
        var history = CreateHistoryWithEdit();
        RecordEdit(history, 2);

        // Act
        history.Undo();

        // Assert
        await Assert.That(value).IsEqualTo(1);
    }

    [Test]
    public async Task Undo_WithEdit_AllowsRedo()
    {
        // Arrange
        var history = CreateHistoryWithEdit();

        // Act
        history.Undo();

        // Assert
        await Assert.That(history.CanRedo).IsTrue();
    }

    [Test]
    public async Task Undo_WithEdit_RaisesApplied()
    {
        // Arrange
        var history = CreateHistoryWithEdit();
        var raised = false;
        history.Applied += () => raised = true;

        // Act
        history.Undo();

        // Assert
        await Assert.That(raised).IsTrue();
    }

    [Test]
    public async Task Undo_WithFailingStep_ReturnsFalse()
    {
        // Arrange
        var history = new ReuObjectTreeHistory();
        RecordFailingEdit(history);

        // Act
        var succeeded = history.Undo();

        // Assert
        await Assert.That(succeeded).IsFalse();
    }

    [Test]
    public async Task Undo_WithFailingStep_KeepsOlderStepsReachable()
    {
        // Arrange
        var history = CreateHistoryWithEdit();
        RecordFailingEdit(history);
        history.Undo();

        // Act
        var succeeded = history.Undo();

        // Assert
        await Assert.That(succeeded).IsTrue();
        await Assert.That(value).IsEqualTo(0);
    }

    [Test]
    public async Task Undo_WithFailingStep_DoesNotAllowRedo()
    {
        // Arrange
        var history = new ReuObjectTreeHistory();
        RecordFailingEdit(history);

        // Act
        history.Undo();

        // Assert
        await Assert.That(history.CanRedo).IsFalse();
    }

    [Test]
    public async Task Undo_WithThrowingStep_ReturnsFalse()
    {
        // Arrange
        var history = new ReuObjectTreeHistory();
        history.Record("Throwing", () => throw new InvalidOperationException(), () => true);

        // Act
        var succeeded = history.Undo();

        // Assert
        await Assert.That(succeeded).IsFalse();
    }

    [Test]
    public async Task Redo_WithoutSteps_ReturnsFalse()
    {
        // Arrange
        var history = CreateHistoryWithEdit();

        // Act
        var succeeded = history.Redo();

        // Assert
        await Assert.That(succeeded).IsFalse();
    }

    [Test]
    public async Task Redo_AfterUndo_ReappliesEdit()
    {
        // Arrange
        var history = CreateHistoryWithEdit();
        history.Undo();

        // Act
        var succeeded = history.Redo();

        // Assert
        await Assert.That(succeeded).IsTrue();
        await Assert.That(value).IsEqualTo(1);
    }

    [Test]
    public async Task Redo_AfterUndo_AllowsUndoAgain()
    {
        // Arrange
        var history = CreateHistoryWithEdit();
        history.Undo();
        history.Redo();

        // Act
        history.Undo();

        // Assert
        await Assert.That(value).IsEqualTo(0);
    }

    [Test]
    public async Task Clear_WithSteps_DiscardsSteps()
    {
        // Arrange
        var history = CreateHistoryWithEdit();
        RecordEdit(history, 2);
        history.Undo();

        // Act
        history.Clear();

        // Assert
        await Assert.That(history.CanUndo).IsFalse();
        await Assert.That(history.CanRedo).IsFalse();
    }

    [Test]
    public async Task UndoDescription_WithSteps_DescribesLatestStep()
    {
        // Arrange
        var history = CreateHistoryWithEdit();
        RecordEdit(history, 2);

        // Act
        var description = history.UndoDescription;

        // Assert
        await Assert.That(description).IsEqualTo("Set 2");
    }

    [Test]
    public async Task RedoDescription_AfterUndo_DescribesUndoneStep()
    {
        // Arrange
        var history = CreateHistoryWithEdit();
        history.Undo();

        // Act
        var description = history.RedoDescription;

        // Assert
        await Assert.That(description).IsEqualTo("Set 1");
    }
}
