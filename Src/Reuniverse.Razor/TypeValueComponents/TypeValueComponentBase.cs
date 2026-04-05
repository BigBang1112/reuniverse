using Microsoft.AspNetCore.Components;
using System.Reflection;

namespace Reuniverse.Razor.TypeValueComponents;

public interface ITypeValueComponent
{
    object? Value { get; set; }
}

public abstract class TypeValueComponentBase<T> : ComponentBase, ITypeValueComponent
{
    private T? tempValue;

    [Parameter]
    public ReuObjectTreeMember? Member { get; set; }

    [Parameter]
    public ReuObjectTreeItem? Item { get; set; }

    [Parameter]
    public ReuObjectTreeKeyValue? KeyValue { get; set; }

    [Parameter]
    public EventCallback ValueChanged { get; set; }

    [Parameter, EditorRequired]
    public Dictionary<Type, Type> TypeValueComponents { get; set; }

    [Parameter]
    public bool IsKey { get; set; }

    /// <summary>
    /// Value to use instead of getting/setting from Member/Item/KeyValue.
    /// </summary>
    [Parameter]
    public T? ReadOnlyValue { get; set; }

    [Parameter]
    public T? DefaultValue { get; set; }

    [Parameter]
    public string? Placeholder { get; set; }

    public PropertyInfo? Property => Member?.Property;
    public object? Parent => Member?.Parent ?? Item?.Parent;

    public virtual T? Value
    {
        get
        {
            if (ReadOnlyValue is not null) return ReadOnlyValue;
            if (Property is not null) return (T?)Property.GetValue(Parent);
            if (Item is not null) return (T?)Item.Value;
            if (KeyValue is not null) return IsKey ? (T?)KeyValue.Key : (T?)KeyValue.Value;
            return tempValue ?? DefaultValue;
        }
        set
        {
            if (Item is not null)
            {
                if (Item.SetValue(value))
                {
                    ValueChanged.InvokeAsync();
                }

                return;
            }

            if (Property is not null)
            {
                var oldValue = Value;

                try
                {
                    Property.SetValue(Parent, value);
                    Exception = null;

                    // Only invoke ValueChanged if the value actually changed
                    if (!Equals(oldValue, value))
                    {
                        ValueChanged.InvokeAsync();
                    }
                }
                catch (TargetInvocationException ex)
                {
                    Exception = ex.InnerException ?? ex;
                }

                return;
            }

            tempValue = value;
        }
    }

    public Exception? Exception { get; protected set; }

    public bool IsReadOnly
    {
        get
        {
            if (Item is not null)
            {
                var collectionType = Item.Parent.GetType();
                if (collectionType.IsArray)
                {
                    return false;
                }

                if (Item.Parent is System.Collections.IList)
                {
                    return false;
                }
            }

            if (Property is not null)
            {
                if (Property.SetMethod is null) // does not consider init;
                {
                    return true;
                }

                var parentType = Parent?.GetType();

                // not sure what happens if two parents, one readonly, one non-readonly child
                if (parentType is not null && parentType.IsValueType && !parentType.IsPrimitive && !parentType.IsEnum)
                {
                    return Attribute.IsDefined(parentType, typeof(System.Runtime.CompilerServices.IsReadOnlyAttribute));
                }

                return false;
            }

            return (tempValue ?? DefaultValue) is null;
        }
    }

    object? ITypeValueComponent.Value
    {
        get => Value;
        set => Value = (T?)value;
    }
}
