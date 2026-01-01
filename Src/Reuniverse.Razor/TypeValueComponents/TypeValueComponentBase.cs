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

    public abstract T? Value { get; set; }

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

                if (Item.Parent is System.Collections.ICollection)
                {
                    return false;
                }
            }

            return Property?.SetMethod is null;
        }
    }
}
