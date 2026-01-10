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
}