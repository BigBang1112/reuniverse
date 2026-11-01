using AngleSharp.Dom;

namespace Reuniverse.Razor.Tests.TypeValueComponents;

// these components share one shape: a value shown in an input, and a null that resets to the type's default
public class ScalarTypeValueComponentTests : ReuObjectTreeTestBase
{
    private static readonly DateTime SomeDate = new(2026, 1, 2, 3, 4, 5);
    private static readonly DateTimeOffset SomeDateOffset = new(SomeDate, TimeSpan.FromHours(1));
    private static readonly Guid SomeGuid = Guid.Parse("11111111-2222-3333-4444-555555555555");

    private class Model
    {
        public string Text { get; set; } = "text";
        public string? NullText { get; set; }
        public int Number { get; set; } = 1;
        public int? NullNumber { get; set; }
        public byte Small { get; set; } = 1;
        public double Decimal { get; set; } = 1.5;
        public bool Flag { get; set; }
        public DateTime Date { get; set; } = SomeDate;
        public DateTimeOffset DateOffset { get; set; } = SomeDateOffset;
        public Guid Id { get; set; } = SomeGuid;
        public TimeSpan Duration { get; set; } = TimeSpan.FromHours(1);
        public byte[]? Bytes { get; set; }
        public int ReadOnly => 1;
        public string ReadOnlyText => "fixed";
        public bool ReadOnlyFlag => true;
    }

    private static IElement Input(IRenderedComponent<ReuObjectTree> cut, string property)
    {
        return cut.MemberRow(property).QuerySelector("input")!;
    }

    [Test]
    public async Task EditText_WithNewValue_UpdatesProperty()
    {
        // Arrange
        var model = new Model();
        var cut = RenderObjectTree(model);

        // Act
        Input(cut, nameof(Model.Text)).Change("edited");

        // Assert
        await Assert.That(model.Text).IsEqualTo("edited");
    }

    [Test]
    public async Task Render_WithText_ShowsLength()
    {
        // Act
        var cut = RenderObjectTree(new Model());

        // Assert
        await Assert.That(cut.MemberRow(nameof(Model.Text)).QuerySelector("code")!.TextContent).IsEqualTo("4");
    }

    [Test]
    public async Task ClickNull_OnString_SetsEmptyString()
    {
        // Arrange
        var model = new Model();
        var cut = RenderObjectTree(model);

        // Act
        cut.MemberRow(nameof(Model.NullText)).QuerySelector(".box-value.clickable")!.Click();

        // Assert
        await Assert.That(model.NullText).IsEqualTo("");
    }

    [Test]
    public async Task EditNumber_WithNewValue_UpdatesProperty()
    {
        // Arrange
        var model = new Model();
        var cut = RenderObjectTree(model);

        // Act
        Input(cut, nameof(Model.Number)).Change("42");

        // Assert
        await Assert.That(model.Number).IsEqualTo(42);
    }

    [Test]
    public async Task EditNumber_WithByte_UpdatesProperty()
    {
        // Arrange
        var model = new Model();
        var cut = RenderObjectTree(model);

        // Act
        Input(cut, nameof(Model.Small)).Change("200");

        // Assert
        await Assert.That(model.Small).IsEqualTo((byte)200);
    }

    [Test]
    public async Task EditNumber_WithDecimalPoint_UsesInvariantCulture()
    {
        // Arrange
        var model = new Model();
        var cut = RenderObjectTree(model);

        // Act
        Input(cut, nameof(Model.Decimal)).Change("2.25");

        // Assert
        await Assert.That(model.Decimal).IsEqualTo(2.25);
    }

    [Test]
    public async Task ClickNull_OnNullableNumber_SetsDefault()
    {
        // Arrange
        var model = new Model();
        var cut = RenderObjectTree(model);

        // Act
        cut.MemberRow(nameof(Model.NullNumber)).QuerySelector(".box-value.clickable")!.Click();

        // Assert
        await Assert.That(model.NullNumber).IsEqualTo(0);
    }

    [Test]
    [Arguments(nameof(Model.ReadOnly))]
    [Arguments(nameof(Model.ReadOnlyText))]
    public async Task Render_WithGetOnlyProperty_MakesInputReadOnly(string property)
    {
        // Act
        var cut = RenderObjectTree(new Model());

        // Assert
        await Assert.That(Input(cut, property).HasAttribute("readonly")).IsTrue();
    }

    [Test]
    public async Task Render_WithSettableProperty_KeepsInputEditable()
    {
        // Act
        var cut = RenderObjectTree(new Model());

        // Assert
        await Assert.That(Input(cut, nameof(Model.Number)).HasAttribute("readonly")).IsFalse();
    }

    [Test]
    public async Task ToggleFlag_WhenUnchecked_UpdatesProperty()
    {
        // Arrange
        var model = new Model();
        var cut = RenderObjectTree(model);

        // Act
        Input(cut, nameof(Model.Flag)).Change(true);

        // Assert
        await Assert.That(model.Flag).IsTrue();
    }

    [Test]
    public async Task Render_WithGetOnlyFlag_DisablesCheckbox()
    {
        // Act
        var cut = RenderObjectTree(new Model());

        // Assert
        await Assert.That(Input(cut, nameof(Model.ReadOnlyFlag)).HasAttribute("disabled")).IsTrue();
    }

    [Test]
    public async Task EditDate_WithNewValue_UpdatesProperty()
    {
        // Arrange
        var model = new Model();
        var cut = RenderObjectTree(model);

        // Act
        Input(cut, nameof(Model.Date)).Change("2030-05-06 07:08:09");

        // Assert
        await Assert.That(model.Date).IsEqualTo(new DateTime(2030, 5, 6, 7, 8, 9));
    }

    [Test]
    public async Task Render_WithDate_UsesSortableFormat()
    {
        // Act
        var cut = RenderObjectTree(new Model());

        // Assert
        await Assert.That(Input(cut, nameof(Model.Date)).GetAttribute("value")).IsEqualTo(SomeDate.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFF"));
    }

    [Test]
    public async Task EditDateOffset_WithNewValue_UpdatesProperty()
    {
        // Arrange
        var model = new Model();
        var cut = RenderObjectTree(model);

        // Act
        Input(cut, nameof(Model.DateOffset)).Change("2030-05-06 07:08:09 +02:00");

        // Assert
        await Assert.That(model.DateOffset).IsEqualTo(new DateTimeOffset(2030, 5, 6, 7, 8, 9, TimeSpan.FromHours(2)));
    }

    [Test]
    public async Task EditId_WithNewValue_UpdatesProperty()
    {
        // Arrange
        var model = new Model();
        var cut = RenderObjectTree(model);
        var edited = Guid.Parse("99999999-8888-7777-6666-555555555555");

        // Act
        Input(cut, nameof(Model.Id)).Change(edited.ToString());

        // Assert
        await Assert.That(model.Id).IsEqualTo(edited);
    }

    [Test]
    public async Task EditDuration_WithNewValue_UpdatesProperty()
    {
        // Arrange
        var model = new Model();
        var cut = RenderObjectTree(model);

        // Act
        Input(cut, nameof(Model.Duration)).Change("02:03:04");

        // Assert
        await Assert.That(model.Duration).IsEqualTo(new TimeSpan(2, 3, 4));
    }

    [Test]
    public async Task Render_WithBytes_ShowsSize()
    {
        // Act
        var cut = RenderObjectTree(new Model { Bytes = new byte[12] });

        // Assert
        await Assert.That(cut.MemberRow(nameof(Model.Bytes)).QuerySelector("code")!.TextContent).IsEqualTo("12 B");
    }

    [Test]
    public async Task Render_WithNullBytes_ShowsNull()
    {
        // Act
        var cut = RenderObjectTree(new Model());

        // Assert
        await Assert.That(cut.MemberRow(nameof(Model.Bytes)).QuerySelector(".box-value")!.TextContent).IsEqualTo("null");
    }
}
