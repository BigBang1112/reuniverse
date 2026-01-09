namespace Reuniverse.Razor;

public sealed record ReuObjectTreeKeyValue(object Parent, object Key, object? Value)
{
    public override string ToString() => $"{Key} = {Value ?? "null"}";

    /*public bool SetValue(object? newValue)
    {
        if (Parent is System.Collections.IList list)
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
    }*/
}
