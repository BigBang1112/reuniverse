namespace Reuniverse.Razor;

public sealed record ReuObjectTreeItem(object Parent, int Index, object? Value) : IReuObjectTreeElement
{
    public object? Value { get; private set; } = Value;

    public override string ToString() => $"#{Index} {Value ?? "null"}";

    public bool SetValue(object? newValue)
    {
        if (Parent is System.Collections.IList list)
        {
            // the list could have changed since rendering, so the index may be another element now
            if (Index >= list.Count || !Equals(list[Index], Value))
            {
                throw new InvalidOperationException("The list has changed since this element was shown, so the edit was not applied.");
            }

            newValue ??= GetDefaultElement();

            if (list[Index]?.Equals(newValue) == true)
            {
                return false;
            }
            list[Index] = newValue;
            Value = newValue;
            return true;
        }
        else
        {
            throw new InvalidOperationException("Parent is not an array or a list.");
        }
    }

    // lists reject null for value type elements, while arrays and properties reset to default
    private object? GetDefaultElement()
    {
        var elementType = Parent.GetType().GetInterfaces()
            .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IList<>))?
            .GenericTypeArguments[0];

        return elementType is { IsValueType: true } && Nullable.GetUnderlyingType(elementType) is null
            ? Activator.CreateInstance(elementType)
            : null;
    }
}
