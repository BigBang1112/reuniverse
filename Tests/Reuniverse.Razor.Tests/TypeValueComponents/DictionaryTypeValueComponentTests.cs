namespace Reuniverse.Razor.Tests.TypeValueComponents;

public class DictionaryTypeValueComponentTests : ReuObjectTreeTestBase
{
    private enum NoZero
    {
        First = 1,
        Second = 2
    }

    [Flags]
    private enum Permissions
    {
        None = 0,
        Read = 1,
        Write = 2
    }

    [Test]
    [Arguments(1, "1 element")]
    [Arguments(2, "2 elements")]
    public async Task Render_WithDictionary_ShowsElementCount(int count, string expected)
    {
        // Act
        var cut = RenderObjectTree(Enumerable.Range(0, count).ToDictionary(x => x));

        // Assert
        await Assert.That(cut.Find(".box-value").TextContent).IsEqualTo(expected);
    }

    [Test]
    public async Task AddElement_WithStringKey_AddsTypedKey()
    {
        // Arrange
        var dictionary = new Dictionary<string, int>();
        var cut = RenderObjectTree(dictionary);
        cut.Find("input").Change("key");

        // Act
        cut.Find(".add").Click();

        // Assert
        await dictionary.Keys.ShouldEqualInOrderAsync("key");
    }

    [Test]
    public async Task AddElement_WithDuplicateKey_ShowsError()
    {
        // Arrange
        var cut = RenderObjectTree(new Dictionary<string, int> { [""] = 1 });

        // Act
        cut.Find(".add").Click();

        // Assert
        await Assert.That(cut.FindAll(".exception").Count).IsEqualTo(1);
    }

    [Test]
    public async Task AddElement_WithReferenceTypeValue_AddsNullValue()
    {
        // Arrange
        var dictionary = new Dictionary<string, string?>();
        var cut = RenderObjectTree(dictionary);

        // Act
        cut.Find(".add").Click();

        // Assert
        await Assert.That(dictionary[""]).IsNull();
    }

    [Test]
    public async Task Render_WithNonGenericDictionary_HidesAddButton()
    {
        // Act
        var cut = RenderObjectTree(new System.Collections.Hashtable());

        // Assert
        await Assert.That(cut.FindAll(".add").Count).IsEqualTo(0);
    }

    [Test]
    public async Task AddElement_WithDefaultEnumKey_AddsFirstKey()
    {
        // Arrange
        var dictionary = new Dictionary<DayOfWeek, int>();
        var cut = RenderObjectTree(dictionary);

        // Act
        cut.Find(".add").Click();

        // Assert
        await dictionary.Keys.ShouldEqualInOrderAsync(DayOfWeek.Sunday);
    }

    [Test]
    public async Task AddElement_WithPickedEnumKey_AddsPickedKey()
    {
        // Arrange
        var dictionary = new Dictionary<DayOfWeek, int>();
        var cut = RenderObjectTree(dictionary);
        cut.Find("select").Change("3");

        // Act
        cut.Find(".add").Click();

        // Assert
        await dictionary.Keys.ShouldEqualInOrderAsync(DayOfWeek.Wednesday);
    }

    [Test]
    public async Task AddElement_WithoutZeroEnumValue_AddsFirstDefinedKey()
    {
        // Arrange
        var dictionary = new Dictionary<NoZero, int>();
        var cut = RenderObjectTree(dictionary);

        // Act
        cut.Find(".add").Click();

        // Assert
        await dictionary.Keys.ShouldEqualInOrderAsync(NoZero.First);
    }

    [Test]
    public async Task AddElement_WithCombinedFlags_AddsCombinedKey()
    {
        // Arrange
        var dictionary = new Dictionary<Permissions, int>();
        var cut = RenderObjectTree(dictionary);
        cut.Find("select").Change("1");
        cut.Find("select").Change("2");

        // Act
        cut.Find(".add").Click();

        // Assert
        await dictionary.Keys.ShouldEqualInOrderAsync(Permissions.Read | Permissions.Write);
    }

    [Test]
    public async Task AddElement_WithPickedEnumKey_ResetsKeyEditor()
    {
        // Arrange
        var cut = RenderObjectTree(new Dictionary<DayOfWeek, int>());
        cut.Find("select").Change("3");

        // Act
        cut.Find(".add").Click();

        // Assert
        await Assert.That(cut.Find("select").GetAttribute("value")).IsEqualTo("0");
    }

    [Test]
    public async Task ChangeKey_WithEnumKey_SelectsPickedValue()
    {
        // Arrange
        var cut = RenderObjectTree(new Dictionary<DayOfWeek, int>());

        // Act
        cut.Find("select").Change("3");

        // Assert
        await Assert.That(cut.Find("select").GetAttribute("value")).IsEqualTo("3");
    }

    [Test]
    public async Task Render_WithExistingEnumKeys_ShowsRowsAsPlainText()
    {
        // Act
        var cut = RenderObjectTree(new Dictionary<DayOfWeek, int> { [DayOfWeek.Friday] = 1 });

        // Assert
        await Assert.That(cut.FindAll("select").Count).IsEqualTo(1);
        await Assert.That(cut.Markup).Contains("Friday");
    }
}
