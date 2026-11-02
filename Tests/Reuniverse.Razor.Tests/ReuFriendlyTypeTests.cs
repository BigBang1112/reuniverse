namespace Reuniverse.Razor.Tests;

public class ReuFriendlyTypeTests : BunitContext
{
    public class Nested;

    private IRenderedComponent<ReuFriendlyType> RenderType(Type type)
    {
        return Render<ReuFriendlyType>(parameters => parameters.Add(x => x.Type, type));
    }

    [Test]
    [Arguments(typeof(int), "int")]
    [Arguments(typeof(int?), "int?")]
    [Arguments(typeof(int[]), "int[]")]
    [Arguments(typeof(int[,]), "int[,]")]
    [Arguments(typeof(DateTime), "DateTime")]
    [Arguments(typeof(List<int>), "List<int>")]
    [Arguments(typeof(Dictionary<string, int>), "Dictionary<string,int>")]
    [Arguments(typeof(Nested), "ReuFriendlyTypeTests.Nested")]
    public async Task Render_WithType_ShowsFriendlyName(Type type, string expected)
    {
        // Act
        var cut = RenderType(type);

        // Assert
        var text = string.Concat(cut.Find("*").Parent!.TextContent.Where(c => !char.IsWhiteSpace(c)));
        await Assert.That(text).IsEqualTo(expected);
    }

    [Test]
    public async Task Render_WithGenericParameter_MarksParameter()
    {
        // Act
        var cut = RenderType(typeof(List<>).GetGenericArguments()[0]);

        // Assert
        await Assert.That(cut.Find(".generic").TextContent).IsEqualTo("T");
    }

    [Test]
    [Arguments(typeof(int), "keyword")]
    [Arguments(typeof(DayOfWeek), "enum")]
    [Arguments(typeof(DateTime), "struct")]
    [Arguments(typeof(IDisposable), "interface")]
    [Arguments(typeof(Random), "class")]
    public async Task Render_WithTypeKind_AppliesColorClass(Type type, string expectedClass)
    {
        // Act
        var cut = RenderType(type);

        // Assert
        await Assert.That(cut.Find(".type").ClassList.Contains(expectedClass)).IsTrue();
    }

    [Test]
    public async Task Click_OnGenericArgument_ReportsArgumentType()
    {
        // Arrange
        Type? clicked = null;
        var cut = Render<ReuFriendlyType>(parameters => parameters
            .Add(x => x.Type, typeof(List<int>))
            .Add(x => x.OnClick, type => clicked = type));

        // Act
        cut.Find(".type.keyword").Click();

        // Assert
        await Assert.That(clicked).IsEqualTo(typeof(int));
    }
}
