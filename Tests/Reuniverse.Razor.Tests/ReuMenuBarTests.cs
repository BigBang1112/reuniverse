using Microsoft.AspNetCore.Components.Web;

namespace Reuniverse.Razor.Tests;

public class ReuMenuBarTests : BunitContext
{
    private static ReuMenuBarItemData CreateFileMenu(Func<Task>? action = null) => new()
    {
        Name = "File",
        Items =
        [
            new ReuMenuBarItemData { Name = "Open", Action = action },
            new ReuMenuBarItemData { Name = "Recent", Items = [new ReuMenuBarItemData { Name = "Map.Gbx" }] }
        ]
    };

    private static ReuMenuBarItemData CreateEditMenu() => new()
    {
        Name = "Edit",
        Items = [new ReuMenuBarItemData { Name = "Undo" }]
    };

    private IRenderedComponent<ReuMenuBar> RenderMenuBar(params ReuMenuBarItemData[] items)
    {
        return Render<ReuMenuBar>(parameters => parameters.Add(x => x.Items, items));
    }

    private static int DropdownCount(IRenderedComponent<ReuMenuBar> cut) => cut.FindAll(".reu-menubar-dropdown").Count;

    private static void ClickItem(IRenderedComponent<ReuMenuBar> cut, string name)
    {
        cut.FindAll(".reu-menubar-item").First(x => x.TextContent.Trim().StartsWith(name)).Click();
    }

    private static void HoverItem(IRenderedComponent<ReuMenuBar> cut, string name)
    {
        cut.FindAll(".reu-menubar-item").First(x => x.TextContent.Trim().StartsWith(name)).TriggerEvent("onmouseenter", new MouseEventArgs());
    }

    [Test]
    public async Task Render_WithItems_HidesDropdowns()
    {
        // Act
        var cut = RenderMenuBar(CreateFileMenu());

        // Assert
        await Assert.That(DropdownCount(cut)).IsEqualTo(0);
    }

    [Test]
    public async Task Render_WithTemplateContent_UsesTemplate()
    {
        // Act
        var cut = Render<ReuMenuBar>(parameters => parameters
            .Add(x => x.Items, [new ReuMenuBarItemData { Name = "File" }])
            .Add(x => x.TemplateContent, item => $"<b>{item.Name}!</b>"));

        // Assert
        await Assert.That(cut.Find(".reu-menubar-item b").TextContent).IsEqualTo("File!");
    }

    [Test]
    public async Task Render_WithHref_RendersLink()
    {
        // Act
        var cut = RenderMenuBar(new ReuMenuBarItemData { Name = "Docs", Href = "/docs", Target = "_blank" });

        // Assert
        var link = cut.Find("a.reu-menubar-item");
        await Assert.That(link.GetAttribute("href")).IsEqualTo("/docs");
        await Assert.That(link.GetAttribute("target")).IsEqualTo("_blank");
    }

    [Test]
    public async Task ClickItem_WithSubItems_OpensDropdown()
    {
        // Arrange
        var cut = RenderMenuBar(CreateFileMenu());

        // Act
        ClickItem(cut, "File");

        // Assert
        await Assert.That(DropdownCount(cut)).IsEqualTo(1);
    }

    [Test]
    public async Task ClickItem_WhenOpen_ClosesDropdown()
    {
        // Arrange
        var cut = RenderMenuBar(CreateFileMenu());
        ClickItem(cut, "File");

        // Act
        ClickItem(cut, "File");

        // Assert
        await Assert.That(DropdownCount(cut)).IsEqualTo(0);
    }

    [Test]
    public async Task ClickItem_WithoutSubItems_OpensNothing()
    {
        // Arrange
        var cut = RenderMenuBar(new ReuMenuBarItemData { Name = "Help" });

        // Act
        ClickItem(cut, "Help");

        // Assert
        await Assert.That(cut.Instance.SelectedItem).IsNull();
    }

    [Test]
    public async Task ClickItem_WithAction_InvokesAction()
    {
        // Arrange
        var invoked = false;
        var item = new ReuMenuBarItemData { Name = "Help", Action = () => { invoked = true; return Task.CompletedTask; } };
        var cut = RenderMenuBar(item);

        // Act
        ClickItem(cut, "Help");

        // Assert
        await Assert.That(invoked).IsTrue();
    }

    [Test]
    public async Task ClickItem_WithNestedAction_InvokesAction()
    {
        // Arrange
        var invoked = false;
        var cut = RenderMenuBar(CreateFileMenu(() => { invoked = true; return Task.CompletedTask; }));
        ClickItem(cut, "File");

        // Act
        ClickItem(cut, "Open");

        // Assert
        await Assert.That(invoked).IsTrue();
    }

    [Test]
    public async Task HoverItem_WhileMenuOpen_SwitchesDropdown()
    {
        // Arrange
        var edit = CreateEditMenu();
        var cut = RenderMenuBar(CreateFileMenu(), edit);
        ClickItem(cut, "File");

        // Act
        HoverItem(cut, "Edit");

        // Assert
        await Assert.That(cut.Instance.SelectedItem).IsSameReferenceAs(edit);
    }

    [Test]
    public async Task HoverItem_WhileMenuClosed_OpensNothing()
    {
        // Arrange
        var cut = RenderMenuBar(CreateFileMenu());

        // Act
        HoverItem(cut, "File");

        // Assert
        await Assert.That(DropdownCount(cut)).IsEqualTo(0);
    }

    [Test]
    public async Task HoverItem_WithNestedSubItems_OpensNestedDropdown()
    {
        // Arrange
        var cut = RenderMenuBar(CreateFileMenu());
        ClickItem(cut, "File");

        // Act
        HoverItem(cut, "Recent");

        // Assert
        await Assert.That(cut.FindAll(".reu-menubar-dropdown.nested").Count).IsEqualTo(1);
    }

    [Test]
    public async Task Render_WithNestedSubItems_ShowsArrow()
    {
        // Arrange
        var cut = RenderMenuBar(CreateFileMenu());

        // Act
        ClickItem(cut, "File");

        // Assert
        await Assert.That(cut.FindAll(".reu-menubar-dropdown .reu-menubar-item")[1].TextContent).Contains(">");
    }
}
