namespace Reuniverse.Razor.Tests;

public class ReuObjectTreeMemberTests
{
    private class Model
    {
        private string? writeOnly;

        public int Number { get; set; } = 5;
        public int ReadOnly => 7;
        public string? WriteOnly { set => writeOnly = value; }
        public int Throws => throw new InvalidOperationException("getter failed");

        public string? GetWriteOnly() => writeOnly;
    }

    private static ReuObjectTreeMember CreateMember(Model model, string property)
    {
        return new ReuObjectTreeMember(model, typeof(Model).GetProperty(property)!);
    }

    [Test]
    public async Task Value_WithReadableProperty_ReturnsValue()
    {
        // Arrange
        var member = CreateMember(new Model(), nameof(Model.Number));

        // Act
        var value = member.Value;

        // Assert
        await Assert.That(value).IsEqualTo(5);
    }

    [Test]
    public async Task Value_WithWriteOnlyProperty_ReturnsNull()
    {
        // Arrange
        var member = CreateMember(new Model(), nameof(Model.WriteOnly));

        // Act
        var value = member.Value;

        // Assert
        await Assert.That(value).IsNull();
    }

    [Test]
    public async Task Value_WhenSet_UpdatesProperty()
    {
        // Arrange
        var model = new Model();
        var member = CreateMember(model, nameof(Model.Number));

        // Act
        member.Value = 9;

        // Assert
        await Assert.That(model.Number).IsEqualTo(9);
    }

    [Test]
    public async Task Value_WhenSetOnWriteOnlyProperty_UpdatesProperty()
    {
        // Arrange
        var model = new Model();
        var member = CreateMember(model, nameof(Model.WriteOnly));

        // Act
        member.Value = "written";

        // Assert
        await Assert.That(model.GetWriteOnly()).IsEqualTo("written");
    }

    [Test]
    public async Task Value_WhenSetOnReadOnlyProperty_DoesNothing()
    {
        // Arrange
        var model = new Model();
        var member = CreateMember(model, nameof(Model.ReadOnly));

        // Act
        member.Value = 9;

        // Assert
        await Assert.That(model.ReadOnly).IsEqualTo(7);
    }

    [Test]
    public async Task ToString_WithReadableProperty_ShowsTypeNameAndValue()
    {
        // Arrange
        var member = CreateMember(new Model(), nameof(Model.Number));

        // Act
        var text = member.ToString();

        // Assert
        await Assert.That(text).IsEqualTo("int Number: 5");
    }

    [Test]
    public async Task ToString_WithThrowingGetter_DoesNotThrow()
    {
        // Arrange
        var member = CreateMember(new Model(), nameof(Model.Throws));

        // Act
        var text = member.ToString();

        // Assert
        await Assert.That(text).StartsWith("int Throws: ");
    }
}
