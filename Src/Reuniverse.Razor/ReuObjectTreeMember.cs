using System.Reflection;

namespace Reuniverse.Razor;

public sealed record ReuObjectTreeMember(object Parent, PropertyInfo Property)
{
    public object? Value
    {
        get => Property.GetMethod is null ? null : Property.GetValue(Parent);
        set
        {
            if (Property.SetMethod is not null)
            {
                Property.SetValue(Parent, value);
            }
        }
    }

    public override string ToString()
    {
        object? value;

        try
        {
            value = Value;
        }
        catch (Exception ex)
        {
            value = ex.Message;
        }

        return $"{Property.PropertyType.GetFriendlyName()} {Property.Name}: {value}";
    }    
}