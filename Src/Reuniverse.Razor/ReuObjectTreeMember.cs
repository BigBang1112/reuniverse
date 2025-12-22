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

        return $"{GetFriendlyTypeName()} {Property.Name}: {value}";
    }

    public string GetFriendlyTypeName() => GetFriendlyTypeName(Property.PropertyType);

    private static string GetFriendlyTypeName(Type type)
    {
        if (type == typeof(int))
            return "int";
        else if (type == typeof(short))
            return "short";
        else if (type == typeof(byte))
            return "byte";
        else if (type == typeof(bool))
            return "bool";
        else if (type == typeof(long))
            return "long";
        else if (type == typeof(float))
            return "float";
        else if (type == typeof(double))
            return "double";
        else if (type == typeof(decimal))
            return "decimal";
        else if (type == typeof(string))
            return "string";
        else if (type.IsGenericType)
        {
            var typeName = type.Name.Substring(0, type.Name.IndexOf('`'));

            if (typeName == "Nullable")
                return GetFriendlyTypeName(type.GetGenericArguments()[0]) + "?";
            else
                return $"{typeName}<{string.Join(", ", type.GetGenericArguments().Select(GetFriendlyTypeName).ToArray())}>";
        }
        else
            return type.Name;
    }
}