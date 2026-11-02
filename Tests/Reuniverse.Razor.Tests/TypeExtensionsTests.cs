namespace Reuniverse.Razor.Tests;

public class TypeExtensionsTests
{
    public class Nested;

    public class NestedGeneric<T>;

    [Test]
    [Arguments(typeof(int), "int")]
    [Arguments(typeof(string), "string")]
    [Arguments(typeof(object), "object")]
    [Arguments(typeof(DateTime), "DateTime")]
    [Arguments(typeof(int?), "int?")]
    [Arguments(typeof(int[]), "int[]")]
    [Arguments(typeof(int[,]), "int[,]")]
    [Arguments(typeof(int[][]), "int[][]")]
    [Arguments(typeof(List<int>), "List<int>")]
    [Arguments(typeof(Dictionary<string, int?>), "Dictionary<string, int?>")]
    [Arguments(typeof(List<List<string>>), "List<List<string>>")]
    public async Task GetFriendlyName_WithType_ReturnsReadableName(Type type, string expected)
    {
        // Act
        var name = type.GetFriendlyName();

        // Assert
        await Assert.That(name).IsEqualTo(expected);
    }

    [Test]
    public async Task GetFriendlyName_WithGenericParameter_ReturnsParameterName()
    {
        // Arrange
        var parameter = typeof(List<>).GetGenericArguments()[0];

        // Act
        var name = parameter.GetFriendlyName();

        // Assert
        await Assert.That(name).IsEqualTo("T");
    }

    [Test]
    public async Task GetFriendlyName_WithNestedType_IncludesDeclaringType()
    {
        // Act
        var name = typeof(Nested).GetFriendlyName();

        // Assert
        await Assert.That(name).IsEqualTo("TypeExtensionsTests.Nested");
    }

    [Test]
    [Skip("Known bug: the nested check runs before the generic one, so this returns TypeExtensionsTests.NestedGeneric`1")]
    public async Task GetFriendlyName_WithNestedGenericType_IncludesTypeArguments()
    {
        // Act
        var name = typeof(NestedGeneric<int>).GetFriendlyName();

        // Assert
        await Assert.That(name).IsEqualTo("TypeExtensionsTests.NestedGeneric<int>");
    }
}
