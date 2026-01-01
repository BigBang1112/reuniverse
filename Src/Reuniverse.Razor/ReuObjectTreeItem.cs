namespace Reuniverse.Razor;

public sealed record ReuObjectTreeItem(object Parent, int Index, object? Value)
{
    public object? Value { get; private set; } = Value;

    public override string ToString() => $"#{Index} {Value ?? "null"}";

    public bool SetValue(object? newValue)
    {
        var parentType = Parent.GetType();
        if (parentType.IsArray)
        {
            var array = (Array)Parent;
            if (array.GetValue(Index)?.Equals(newValue) == true)
            {
                return false;
            }
            array.SetValue(newValue, Index);
            Value = newValue;
            return true;
        }
        else if (Parent is System.Collections.IList list)
        {
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
}
