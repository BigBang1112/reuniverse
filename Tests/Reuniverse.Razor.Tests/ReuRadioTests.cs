namespace Reuniverse.Razor.Tests;

public class ReuRadioTests : BunitContext
{
    [Test]
    public async Task Render_WithName_GroupsInput()
    {
        // Act
        var cut = Render<ReuRadio>(parameters => parameters.Add(x => x.Name, "group"));

        // Assert
        await Assert.That(cut.Find("input").GetAttribute("name")).IsEqualTo("group");
    }

    [Test]
    public async Task Change_WhenSelected_ReportsChecked()
    {
        // Arrange
        bool? isChecked = null;
        var cut = Render<ReuRadio>(parameters => parameters
            .Add(x => x.Name, "group")
            .Add(x => x.IsCheckedChanged, value => isChecked = value));

        // Act
        cut.Find("input").Change(true);

        // Assert
        await Assert.That(isChecked).IsTrue();
    }
}
