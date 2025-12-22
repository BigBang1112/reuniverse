namespace Reuniverse.Razor;

internal static class TypeExtensions
{
    internal static readonly Dictionary<Type, string> Keywords = new()
    {
        { typeof(bool), "bool" },
        { typeof(byte), "byte" },
        { typeof(sbyte), "sbyte" },
        { typeof(char), "char" },
        { typeof(decimal), "decimal" },
        { typeof(double), "double" },
        { typeof(float), "float" },
        { typeof(int), "int" },
        { typeof(uint), "uint" },
        { typeof(long), "long" },
        { typeof(ulong), "ulong" },
        { typeof(short), "short" },
        { typeof(ushort), "ushort" },
        { typeof(object), "object" },
        { typeof(string), "string" },
        { typeof(void), "void" }
    };

    public static string GetFriendlyName(this Type type)
    {
        // Built-in aliases
        if (Keywords.TryGetValue(type, out var alias))
        {
            return alias;
        }

        // Nullable<T> → T?
        if (Nullable.GetUnderlyingType(type) is Type nullable)
        {
            return $"{nullable.GetFriendlyName()}?";
        }

        // Arrays
        if (type.IsArray)
        {
            var elementType = type.GetElementType()!;
            return $"{elementType.GetFriendlyName()}[{new string(',', type.GetArrayRank() - 1)}]";
        }

        // TKey for example
        if (type.IsGenericParameter)
        {
            return type.Name;
        }

        if (type.IsNested)
        {
            var declaring = type.DeclaringType!;
            return $"{declaring.GetFriendlyName()}.{type.Name}";
        }

        // Generic type
        if (type.IsGenericType)
        {
            var genericTypeDef = type.GetGenericTypeDefinition();
            var genericArgs = type.GetGenericArguments();
            var genericTypeName = genericTypeDef.Name;
            var backtickIndex = genericTypeName.IndexOf('`');
            if (backtickIndex > 0)
            {
                genericTypeName = genericTypeName[..backtickIndex];
            }
            var friendlyArgs = string.Join(", ", genericArgs.Select(t => t.GetFriendlyName()));
            return $"{genericTypeName}<{friendlyArgs}>";
        }

        return type.Name;
    }
}
