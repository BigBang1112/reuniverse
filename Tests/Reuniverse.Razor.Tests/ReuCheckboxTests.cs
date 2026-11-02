namespace Reuniverse.Razor.Tests;

public class ReuCheckboxTests : BunitContext
{
    [Test]
    public async Task Render_WhenChecked_ChecksInput()
    {
        // Act
        var cut = Render<ReuCheckbox>(parameters => parameters.Add(x => x.IsChecked, true));

        // Assert
        await Assert.That(cut.Find("input").HasAttribute("checked")).IsTrue();
    }

    [Test]
    public async Task Render_WithSize_AppliesSizeToCheckmark()
    {
        // Act
        var cut = Render<ReuCheckbox>(parameters => parameters.Add(x => x.Size, 40));

        // Assert
        await Assert.That(cut.Find(".checkmark").GetAttribute("style")).Contains("width:40px");
    }

    [Test]
    public async Task Change_WithBoolValue_ReportsState()
    {
        // Arrange
        bool? isChecked = null;
        var cut = Render<ReuCheckbox>(parameters => parameters.Add(x => x.IsCheckedChanged, value => isChecked = value));

        // Act
        cut.Find("input").Change(true);

        // Assert
        await Assert.That(isChecked).IsTrue();
    }

    [Test]
    public async Task Change_WithNonBoolValue_ReportsNothing()
    {
        // Arrange
        bool? isChecked = null;
        var cut = Render<ReuCheckbox>(parameters => parameters.Add(x => x.IsCheckedChanged, value => isChecked = value));

        // Act
        cut.Find("input").Change("not a bool");

        // Assert
        await Assert.That(isChecked).IsNull();
    }
}
