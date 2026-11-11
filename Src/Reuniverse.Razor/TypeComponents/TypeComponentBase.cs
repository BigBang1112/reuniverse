using Microsoft.AspNetCore.Components;

namespace Reuniverse.Razor.TypeComponents;

public abstract class TypeComponentBase<T> : ComponentBase
{
    [Parameter, EditorRequired]
    public required T Value { get; set; }

    [Parameter, EditorRequired]
    public Dictionary<Type, Type> TypeValueComponents { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnExpandToggle { get; set; }

    [Parameter, EditorRequired]
    public bool IsExpandable { get; set; }

    [Parameter, EditorRequired]
    public bool IsExpanded { get; set; }

    [Parameter, EditorRequired]
    public bool HideType { get; set; }

    [Parameter, EditorRequired]
    public EventCallback<Type> OnTypeClick { get; set; }

    [Parameter, EditorRequired]
    public EventCallback ValueChanged { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnSelect { get; set; }

    [Parameter, EditorRequired]
    public bool Selected { get; set; }

    [Parameter, EditorRequired]
    public Func<object?, RenderFragment?>? VisualizationProvider { get; set; }

    [Parameter, EditorRequired]
    public Func<object?, RenderFragment?>? ExtrasProvider { get; set; }

    /// <summary>
    /// Records the edits made by this component, if the tree keeps a history.
    /// </summary>
    [Parameter]
    public ReuObjectTreeHistory? History { get; set; }

    /// <summary>
    /// Asks before an edit that loses data, such as removing an element. The tree provides it.
    /// Without it, nothing is asked.
    /// </summary>
    [CascadingParameter]
    public ReuDialogService? Dialogs { get; set; }

    /// <summary>
    /// Asks to confirm a destructive edit, or confirms right away when there is nothing to ask with.
    /// </summary>
    protected Task<bool> ConfirmAsync(string message, string confirmText)
    {
        return Dialogs is { HasHost: true } dialogs
            ? dialogs.ConfirmAsync(message, new ReuConfirmOptions { ConfirmText = confirmText, Destructive = true })
            : Task.FromResult(true);
    }

    public Exception? Exception { get; set; }
}