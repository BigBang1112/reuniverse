using System.Collections;
using System.Reflection;

namespace Reuniverse.Razor;

/// <summary>
/// Undo and redo steps for the edits made through a <see cref="ReuObjectTree"/>. Pass the same instance to several
/// trees to share one history between them. Like components, an instance should only be used from the render dispatcher.
/// </summary>
public sealed class ReuObjectTreeHistory
{
    private sealed record Step(string Description, Func<bool> Undo, Func<bool> Redo);

    private readonly List<Step> undoSteps = [];
    private readonly Stack<Step> redoSteps = new();

    /// <summary>
    /// Raised whenever the steps change, including when a new edit is recorded.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Raised after a step was undone or redone, as the edited objects could have changed.
    /// </summary>
    public event Action? Applied;

    /// <summary>
    /// The maximum number of steps that can be undone. Steps keep the old values alive, so older steps are discarded once the limit is reached.
    /// </summary>
    public int Capacity { get; }

    public bool CanUndo => undoSteps.Count > 0;

    public bool CanRedo => redoSteps.Count > 0;

    /// <summary>
    /// A short description of the step <see cref="Undo"/> would revert, if any.
    /// </summary>
    public string? UndoDescription => undoSteps.Count > 0 ? undoSteps[^1].Description : null;

    /// <summary>
    /// A short description of the step <see cref="Redo"/> would reapply, if any.
    /// </summary>
    public string? RedoDescription => redoSteps.TryPeek(out var step) ? step.Description : null;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReuObjectTreeHistory"/> class.
    /// </summary>
    /// <param name="capacity">The maximum number of steps that can be undone.</param>
    public ReuObjectTreeHistory(int capacity = 100)
    {
        Capacity = capacity < 1 ? 1 : capacity;
    }

    /// <summary>
    /// Records an edit that was already made, and discards the steps that could be redone.
    /// Custom value components can use this to take part in the history.
    /// </summary>
    /// <param name="description">A short description of the edit.</param>
    /// <param name="undo">Reverts the edit. Returns false if that is no longer possible.</param>
    /// <param name="redo">Reapplies the edit. Returns false if that is no longer possible.</param>
    public void Record(string description, Func<bool> undo, Func<bool> redo)
    {
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(undo);
        ArgumentNullException.ThrowIfNull(redo);

        AddUndoStep(new Step(description, undo, redo));
        redoSteps.Clear();

        Changed?.Invoke();
    }

    /// <summary>
    /// Reverts the latest step. A step that cannot be reverted anymore, for example because the object
    /// was changed elsewhere, is discarded so that the older steps stay reachable.
    /// </summary>
    /// <returns>True if the step was reverted.</returns>
    public bool Undo()
    {
        if (undoSteps.Count == 0)
        {
            return false;
        }

        var step = undoSteps[^1];
        undoSteps.RemoveAt(undoSteps.Count - 1);

        var succeeded = TryApply(step.Undo);

        if (succeeded)
        {
            redoSteps.Push(step);
        }

        NotifyApplied();
        return succeeded;
    }

    /// <summary>
    /// Reapplies the latest undone step. A step that cannot be reapplied anymore is discarded.
    /// </summary>
    /// <returns>True if the step was reapplied.</returns>
    public bool Redo()
    {
        if (!redoSteps.TryPop(out var step))
        {
            return false;
        }

        var succeeded = TryApply(step.Redo);

        if (succeeded)
        {
            AddUndoStep(step);
        }

        NotifyApplied();
        return succeeded;
    }

    /// <summary>
    /// Discards all steps.
    /// </summary>
    public void Clear()
    {
        undoSteps.Clear();
        redoSteps.Clear();

        Changed?.Invoke();
    }

    internal void RecordProperty(object parent, PropertyInfo property, object? oldValue, object? newValue)
    {
        // structs are edited through a boxed copy, so there is nothing to undo on the actual parent
        if (!property.CanRead || parent.GetType().IsValueType)
        {
            return;
        }

        Record($"Set {property.Name}",
            () => SetProperty(parent, property, oldValue),
            () => SetProperty(parent, property, newValue));
    }

    internal void RecordElementChanged(IList list, int index, object? oldValue, object? newValue)
    {
        Record($"Set #{index}",
            () => ReplaceElement(list, index, newValue, oldValue),
            () => ReplaceElement(list, index, oldValue, newValue));
    }

    internal void RecordElementAdded(IList list, int index, object? value)
    {
        Record($"Add #{index}",
            () => RemoveElement(list, index, value),
            () => InsertElement(list, index, value));
    }

    internal void RecordElementRemoved(IList list, int index, object? value)
    {
        Record($"Remove #{index}",
            () => InsertElement(list, index, value),
            () => RemoveElement(list, index, value));
    }

    internal void RecordKeyAdded(IDictionary dictionary, object key, object? value)
    {
        Record($"Add key {key}",
            () => RemoveKey(dictionary, key),
            () => AddKey(dictionary, key, value));
    }

    internal void RecordKeyRemoved(IDictionary dictionary, object key, object? value)
    {
        Record($"Remove key {key}",
            () => AddKey(dictionary, key, value),
            () => RemoveKey(dictionary, key));
    }

    private void AddUndoStep(Step step)
    {
        undoSteps.Add(step);

        if (undoSteps.Count > Capacity)
        {
            undoSteps.RemoveAt(0);
        }
    }

    private void NotifyApplied()
    {
        Applied?.Invoke();
        Changed?.Invoke();
    }

    private static bool TryApply(Func<bool> apply)
    {
        try
        {
            return apply();
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool SetProperty(object parent, PropertyInfo property, object? value)
    {
        property.SetValue(parent, value);
        return true;
    }

    // list steps depend on the index, so they check it still holds the expected element in case the list was changed elsewhere

    private static bool ReplaceElement(IList list, int index, object? expected, object? value)
    {
        if (index >= list.Count || !Equals(list[index], expected))
        {
            return false;
        }

        list[index] = value;
        return true;
    }

    private static bool InsertElement(IList list, int index, object? value)
    {
        if (index > list.Count)
        {
            return false;
        }

        list.Insert(index, value);
        return true;
    }

    private static bool RemoveElement(IList list, int index, object? expected)
    {
        if (index >= list.Count || !Equals(list[index], expected))
        {
            return false;
        }

        list.RemoveAt(index);
        return true;
    }

    private static bool AddKey(IDictionary dictionary, object key, object? value)
    {
        if (dictionary.Contains(key))
        {
            return false;
        }

        dictionary.Add(key, value);
        return true;
    }

    private static bool RemoveKey(IDictionary dictionary, object key)
    {
        if (!dictionary.Contains(key))
        {
            return false;
        }

        dictionary.Remove(key);
        return true;
    }
}
