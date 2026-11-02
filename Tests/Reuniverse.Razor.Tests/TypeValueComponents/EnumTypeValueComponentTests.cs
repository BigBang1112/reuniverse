namespace Reuniverse.Razor.Tests.TypeValueComponents;

public class EnumTypeValueComponentTests : ReuObjectTreeTestBase
{
    private enum Color
    {
        Red = 1,
        Green = 2
    }

    [Flags]
    private enum Permissions
    {
        None = 0,
        Read = 1,
        Write = 2
    }

    private class Model
    {
        public Color Color { get; set; } = Color.Red;
        public Permissions Permissions { get; set; } = Permissions.Read;
        public Color ReadOnlyColor => Color.Green;
        public Permissions ReadOnlyPermissions => Permissions.Read;
        public Color Undefined { get; set; } = (Color)99;
    }

    [Test]
    public async Task Render_WithValue_SelectsValue()
    {
        // Act
        var cut = RenderObjectTree(new Model());

        // Assert
        await Assert.That(cut.MemberRow(nameof(Model.Color)).QuerySelector("select")!.GetAttribute("value")).IsEqualTo("1");
    }

    [Test]
    public async Task Change_WithOtherValue_UpdatesProperty()
    {
        // Arrange
        var model = new Model();
        var cut = RenderObjectTree(model);

        // Act
        cut.MemberRow(nameof(Model.Color)).QuerySelector("select")!.Change("2");

        // Assert
        await Assert.That(model.Color).IsEqualTo(Color.Green);
    }

    [Test]
    public async Task Render_WithGetOnlyProperty_DisablesSelect()
    {
        // Act
        var cut = RenderObjectTree(new Model());

        // Assert
        await Assert.That(cut.MemberRow(nameof(Model.ReadOnlyColor)).QuerySelector("select")!.HasAttribute("disabled")).IsTrue();
    }

    [Test]
    public async Task Render_WithUndefinedValue_AddsOptionForValue()
    {
        // Act
        var cut = RenderObjectTree(new Model());

        // Assert
        await Assert.That(cut.MemberRow(nameof(Model.Undefined)).QuerySelectorAll("option").Length).IsEqualTo(3);
    }

    [Test]
    public async Task Render_WithFlags_ListsSetFlags()
    {
        // Act
        var cut = RenderObjectTree(new Model { Permissions = Permissions.Read | Permissions.Write });

        // Assert
        await cut.MemberRow(nameof(Model.Permissions)).QuerySelectorAll(".box-value.clickable").Select(x => x.TextContent).ShouldEqualInOrderAsync("Read", "Write");
    }

    [Test]
    public async Task Render_WithFlags_OffersOnlyUnsetFlags()
    {
        // Act
        var cut = RenderObjectTree(new Model());

        // Assert
        await cut.MemberRow(nameof(Model.Permissions)).QuerySelectorAll("option").Select(x => x.TextContent).ShouldEqualInOrderAsync("Add a flag...", "Write");
    }

    [Test]
    public async Task AddFlag_WithUnsetFlag_CombinesFlags()
    {
        // Arrange
        var model = new Model();
        var cut = RenderObjectTree(model);

        // Act
        cut.MemberRow(nameof(Model.Permissions)).QuerySelector("select")!.Change("2");

        // Assert
        await Assert.That(model.Permissions).IsEqualTo(Permissions.Read | Permissions.Write);
    }

    [Test]
    public async Task RemoveFlag_WithSetFlag_ClearsFlag()
    {
        // Arrange
        var model = new Model { Permissions = Permissions.Read | Permissions.Write };
        var cut = RenderObjectTree(model);

        // Act
        cut.MemberRow(nameof(Model.Permissions)).QuerySelector(".box-value.clickable")!.Click();

        // Assert
        await Assert.That(model.Permissions).IsEqualTo(Permissions.Write);
    }

    [Test]
    public async Task Render_WithGetOnlyFlags_OffersNoEditing()
    {
        // Act
        var cut = RenderObjectTree(new Model());

        // Assert
        await Assert.That(cut.MemberRow(nameof(Model.ReadOnlyPermissions)).QuerySelectorAll("select").Length).IsEqualTo(0);
    }
}
