using Microsoft.AspNetCore.Components;
using System.Reflection;

namespace Reuniverse.Razor.TypeValueComponents;

public abstract class TypeValueComponentBase<T> : ComponentBase
{
    [Parameter]
    public ReuObjectTreeMember? Member { get; set; }

    [Parameter]
    public ReuObjectTreeItem? Item { get; set; }

    [Parameter]
    public EventCallback ValueChanged { get; set; }

    public PropertyInfo? Property => Member?.Property;
    public object? Parent => Member?.Parent ?? Item?.Parent;

    public virtual T? Value
    {
        get
        {
            if (Property is not null) return (T?)Property.GetValue(Parent);
            if (Item is not null) return (T?)Item.Value;
            return default;
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

            if (Property is null) return;

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

            if (Property?.SetMethod is null) // does not consider init;
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
    }
}
