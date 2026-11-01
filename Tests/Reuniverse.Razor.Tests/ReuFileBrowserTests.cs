using Microsoft.AspNetCore.Components;

namespace Reuniverse.Razor.Tests;

public class ReuFileBrowserTests : BunitContext
{
    private sealed record TestFile(string Name, long? Size = null, DateTimeOffset? LastModified = null) : IReuFile;

    private sealed class TestFolder(string name, params IReuEntry[] entries) : IReuFolder
    {
        public string Name { get; } = name;
        public IEnumerable<IReuEntry> Entries { get; } = entries;
    }

    private readonly TestFolder documents = new("Documents", new TestFile("report.txt"));
    private readonly TestFile readme = new("readme.md");

    public ReuFileBrowserTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<ReuFileBrowser> RenderBrowser(IReuFolder root, Action<ComponentParameterCollectionBuilder<ReuFileBrowser>>? configure = null)
    {
        return Render<ReuFileBrowser>(parameters =>
        {
            parameters.Add(x => x.Root, root);
            configure?.Invoke(parameters);
        });
    }

    private TestFolder CreateRoot() => new("", readme, documents);

    private static IEnumerable<string> EntryNames(IRenderedComponent<ReuFileBrowser> cut)
    {
        return cut.FindAll(".entry .name").Select(x => x.TextContent);
    }

    private static void ClickEntry(IRenderedComponent<ReuFileBrowser> cut, string name)
    {
        cut.FindAll(".entry").First(x => x.QuerySelector(".name")!.TextContent == name).Click();
    }

    [Test]
    public async Task Render_WithEntries_ListsFoldersBeforeFiles()
    {
        // Act
        var cut = RenderBrowser(CreateRoot());

        // Assert
        await EntryNames(cut).ShouldEqualInOrderAsync("Documents", "readme.md");
    }

    [Test]
    public async Task Render_WithEntries_SortsNamesIgnoringCase()
    {
        // Act
        var cut = RenderBrowser(new TestFolder("", new TestFile("b"), new TestFile("A"), new TestFile("C")));

        // Assert
        await EntryNames(cut).ShouldEqualInOrderAsync("A", "b", "C");
    }

    [Test]
    public async Task Render_WithEmptyFolder_ShowsEmptyMessage()
    {
        // Act
        var cut = RenderBrowser(new TestFolder("Empty"));

        // Assert
        await Assert.That(cut.Find(".empty").TextContent).IsEqualTo("This folder is empty");
    }

    [Test]
    [Arguments("", "Root")]
    [Arguments("Games", "Games")]
    public async Task Render_WithRootName_ShowsBreadcrumb(string name, string expected)
    {
        // Act
        var cut = RenderBrowser(new TestFolder(name));

        // Assert
        await Assert.That(cut.Find(".crumb").TextContent.Trim()).IsEqualTo(expected);
    }

    [Test]
    public async Task Render_AtRoot_DisablesUpButton()
    {
        // Act
        var cut = RenderBrowser(CreateRoot());

        // Assert
        await Assert.That(cut.Find(".up-one-level").HasAttribute("disabled")).IsTrue();
    }

    [Test]
    [Arguments(null, "")]
    [Arguments(512L, "512 B")]
    public async Task Render_WithFileSize_ShowsWholeSize(long? size, string expected)
    {
        // Act
        var cut = RenderBrowser(new TestFolder("", new TestFile("file", size)));

        // Assert
        await Assert.That(cut.Find(".entry .col-size").TextContent).IsEqualTo(expected);
    }

    [Test]
    [Arguments(2048L, 2.0, "kB")]
    [Arguments(3145728L, 3.0, "MB")]
    [Arguments(5368709120L, 5.0, "GB")]
    public async Task Render_WithLargeFileSize_ShowsScaledSize(long size, double scaled, string unit)
    {
        // Act
        var cut = RenderBrowser(new TestFolder("", new TestFile("file", size)));

        // Assert
        await Assert.That(cut.Find(".entry .col-size").TextContent).IsEqualTo($"{scaled:F2} {unit}");
    }

    [Test]
    public async Task Render_WithLastModified_ShowsLocalTime()
    {
        // Arrange
        var modified = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

        // Act
        var cut = RenderBrowser(new TestFolder("", new TestFile("file", LastModified: modified)));

        // Assert
        await Assert.That(cut.Find(".entry .col-modified").TextContent).IsEqualTo(modified.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    [Test]
    public async Task ClickEntry_Once_SelectsEntry()
    {
        // Arrange
        var cut = RenderBrowser(CreateRoot());

        // Act
        ClickEntry(cut, "readme.md");

        // Assert
        await Assert.That(cut.Instance.SelectedEntry).IsSameReferenceAs(readme);
    }

    [Test]
    public async Task ClickEntry_Once_InvokesOnSelect()
    {
        // Arrange
        IReuEntry? selected = null;
        var cut = RenderBrowser(CreateRoot(), parameters => parameters.Add(x => x.OnSelect, entry => selected = entry));

        // Act
        ClickEntry(cut, "readme.md");

        // Assert
        await Assert.That(selected).IsSameReferenceAs(readme);
    }

    [Test]
    public async Task ClickEntry_Once_DoesNotOpenFile()
    {
        // Arrange
        IReuFile? opened = null;
        var cut = RenderBrowser(CreateRoot(), parameters => parameters.Add(x => x.OnFileOpen, file => opened = file));

        // Act
        ClickEntry(cut, "readme.md");

        // Assert
        await Assert.That(opened).IsNull();
    }

    [Test]
    public async Task ClickEntry_Twice_OpensFile()
    {
        // Arrange
        IReuFile? opened = null;
        var cut = RenderBrowser(CreateRoot(), parameters => parameters.Add(x => x.OnFileOpen, file => opened = file));
        ClickEntry(cut, "readme.md");

        // Act
        ClickEntry(cut, "readme.md");

        // Assert
        await Assert.That(opened).IsSameReferenceAs(readme);
    }

    [Test]
    public async Task ClickEntry_TwiceOnFolder_ShowsFolderContents()
    {
        // Arrange
        var cut = RenderBrowser(CreateRoot());
        ClickEntry(cut, "Documents");

        // Act
        ClickEntry(cut, "Documents");

        // Assert
        await EntryNames(cut).ShouldEqualInOrderAsync("report.txt");
    }

    [Test]
    public async Task ClickEntry_TwiceOnFolder_InvokesOnFolderOpen()
    {
        // Arrange
        IReuFolder? opened = null;
        var cut = RenderBrowser(CreateRoot(), parameters => parameters.Add(x => x.OnFolderOpen, folder => opened = folder));
        ClickEntry(cut, "Documents");

        // Act
        ClickEntry(cut, "Documents");

        // Assert
        await Assert.That(opened).IsSameReferenceAs(documents);
    }

    [Test]
    public async Task ClickEntry_TwiceWithDoubleClickToOpen_DoesNotOpen()
    {
        // Arrange
        IReuFile? opened = null;
        var cut = RenderBrowser(CreateRoot(), parameters => parameters
            .Add(x => x.DoubleClickToOpen, true)
            .Add(x => x.OnFileOpen, file => opened = file));
        ClickEntry(cut, "readme.md");

        // Act
        ClickEntry(cut, "readme.md");

        // Assert
        await Assert.That(opened).IsNull();
    }

    [Test]
    public async Task DoubleClickEntry_WithDoubleClickToOpen_OpensFile()
    {
        // Arrange
        IReuFile? opened = null;
        var cut = RenderBrowser(CreateRoot(), parameters => parameters
            .Add(x => x.DoubleClickToOpen, true)
            .Add(x => x.OnFileOpen, file => opened = file));

        // Act
        cut.FindAll(".entry")[1].DoubleClick();

        // Assert
        await Assert.That(opened).IsSameReferenceAs(readme);
    }

    [Test]
    public async Task DoubleClickEntry_WithoutDoubleClickToOpen_DoesNotOpenFile()
    {
        // Arrange
        IReuFile? opened = null;
        var cut = RenderBrowser(CreateRoot(), parameters => parameters.Add(x => x.OnFileOpen, file => opened = file));

        // Act
        cut.FindAll(".entry")[1].DoubleClick();

        // Assert
        await Assert.That(opened).IsNull();
    }

    [Test]
    public async Task NavigateUp_InsideFolder_ShowsParent()
    {
        // Arrange
        var cut = RenderBrowser(CreateRoot());
        ClickEntry(cut, "Documents");
        ClickEntry(cut, "Documents");

        // Act
        cut.Find(".up-one-level").Click();

        // Assert
        await Assert.That(cut.Instance.CurrentFolder!.Name).IsEqualTo("");
    }

    [Test]
    public async Task NavigateTo_WithBreadcrumb_ReturnsToFolder()
    {
        // Arrange
        var inner = new TestFolder("Inner", new TestFile("deep.txt"));
        var outer = new TestFolder("Outer", inner);
        var cut = RenderBrowser(new TestFolder("", outer));
        ClickEntry(cut, "Outer");
        ClickEntry(cut, "Outer");
        ClickEntry(cut, "Inner");
        ClickEntry(cut, "Inner");

        // Act
        cut.FindAll(".crumb")[1].Click();

        // Assert
        await Assert.That(cut.Instance.CurrentFolder).IsSameReferenceAs(outer);
    }

    [Test]
    public async Task NavigateTo_WithCurrentFolder_ClearsNothing()
    {
        // Arrange
        var cut = RenderBrowser(CreateRoot());
        ClickEntry(cut, "readme.md");

        // Act
        cut.Find(".crumb").Click();

        // Assert
        await Assert.That(cut.Instance.SelectedEntry).IsSameReferenceAs(readme);
    }

    [Test]
    public async Task Render_WithNewRoot_ResetsNavigation()
    {
        // Arrange
        var newRoot = new TestFolder("New", new TestFile("new.txt"));
        var cut = RenderBrowser(CreateRoot());
        ClickEntry(cut, "Documents");
        ClickEntry(cut, "Documents");

        // Act
        cut.Render(parameters => parameters.Add(x => x.Root, newRoot));

        // Assert
        await EntryNames(cut).ShouldEqualInOrderAsync("new.txt");
    }

    [Test]
    public async Task Render_WithSameRoot_KeepsNavigation()
    {
        // Arrange
        var root = CreateRoot();
        var cut = RenderBrowser(root);
        ClickEntry(cut, "Documents");
        ClickEntry(cut, "Documents");

        // Act
        cut.Render(parameters => parameters.Add(x => x.Root, root));

        // Assert
        await Assert.That(cut.Instance.CurrentFolder).IsSameReferenceAs(documents);
    }
}
