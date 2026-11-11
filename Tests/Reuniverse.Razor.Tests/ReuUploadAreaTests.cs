using Microsoft.AspNetCore.Components;

namespace Reuniverse.Razor.Tests;

public class ReuUploadAreaTests : ReuUploadTestBase
{
    private IRenderedComponent<ReuUploadArea> RenderUploadArea(long? maxFileSize)
    {
        return Render<ReuUploadArea>(parameters => parameters
            .Add(x => x.MaxFileSize, maxFileSize)
            .Add(x => x.OnUpload, OnUpload)
            .Add(x => x.OnFileTooLarge, OnFileTooLarge));
    }

    private IRenderedComponent<ReuUploadArea> RenderStreamedArea(long? maxFileSize = null)
    {
        return Render<ReuUploadArea>(parameters => parameters
            .Add(x => x.Streamed, true)
            .Add(x => x.MaxFileSize, maxFileSize)
            .Add(x => x.OnStream, OnStream)
            .Add(x => x.OnFileTooLarge, OnFileTooLarge));
    }

    [Test]
    public async Task Render_WhenNotInteractive_ShowsLoader()
    {
        // Arrange
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        // Act
        var cut = Render<ReuUploadArea>();

        // Assert
        await Assert.That(cut.FindAll(".loader").Count).IsEqualTo(1);
    }

    [Test]
    [Arguments(null, true)]
    [Arguments(2, true)]
    [Arguments(1, false)]
    public async Task Render_WithMaxFileCount_SetsMultiple(int? maxFileCount, bool expected)
    {
        // Act
        var cut = Render<ReuUploadArea>(parameters => parameters.Add(x => x.MaxFileCount, maxFileCount));

        // Assert
        await Assert.That(cut.Find("input").HasAttribute("multiple")).IsEqualTo(expected);
    }

    [Test]
    public async Task DragEnter_OverInput_MarksArea()
    {
        // Arrange
        var cut = Render<ReuUploadArea>();

        // Act
        cut.Find("input").DragEnter();

        // Assert
        await Assert.That(cut.Find(".upload-area").ClassList.Contains("drag")).IsTrue();
    }

    [Test]
    public async Task DragLeave_AfterDragEnter_UnmarksArea()
    {
        // Arrange
        var cut = Render<ReuUploadArea>();
        cut.Find("input").DragEnter();

        // Act
        cut.Find("input").DragLeave();

        // Assert
        await Assert.That(cut.Find(".upload-area").ClassList.Contains("drag")).IsFalse();
    }

    [Test]
    [Arguments(null, 0)]
    [Arguments(3, 3)]
    public async Task GetMaxFileCountAsync_WithLimit_ReturnsLimitOrZero(int? maxFileCount, int expected)
    {
        // Arrange
        var cut = Render<ReuUploadArea>(parameters => parameters.Add(x => x.MaxFileCount, maxFileCount));

        // Act
        var result = await cut.Instance.GetMaxFileCountAsync();

        // Assert
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [Arguments(null, 0L)]
    [Arguments(3L, 3L)]
    public async Task GetMaxFileSizeAsync_WithLimit_ReturnsLimitOrZero(long? maxFileSize, long expected)
    {
        // Arrange
        var cut = RenderUploadArea(maxFileSize);

        // Act
        var result = await cut.Instance.GetMaxFileSizeAsync();

        // Assert
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task OnFileTooLargeAsync_FromScript_InvokesCallback()
    {
        // Arrange
        var cut = RenderUploadArea(maxFileSize: 10);

        // Act
        await cut.Instance.OnFileTooLargeAsync("file.bin", 11, 0, "application/octet-stream", null);

        // Assert
        await Assert.That(TooLarge[0].FileName).IsEqualTo("file.bin");
    }

    [Test]
    public async Task OnFileExceedCountAsync_FromScript_InvokesCallback()
    {
        // Arrange
        var cut = Render<ReuUploadArea>(parameters => parameters.Add(x => x.OnFileExceedCount, OnFileExceedCount));

        // Act
        await cut.Instance.OnFileExceedCountAsync("file.bin", 11, 0, "application/octet-stream", null);

        // Assert
        await Assert.That(ExceededCount[0].FileName).IsEqualTo("file.bin");
    }

    [Test]
    public async Task OpenStreamAsync_WithFile_InvokesOnStreamWithSeekableStream()
    {
        // Arrange
        var cut = RenderStreamedArea();

        // Act
        await OpenStreamAsync(cut.Instance.OpenStreamAsync, size: 600_000_000);

        // Assert
        var args = Streamed.Single();
        await Assert.That(args.FileName).IsEqualTo("file.bin");
        await Assert.That(args.Size).IsEqualTo(600_000_000);
        await Assert.That(args.Stream).IsTypeOf<ReuBrowserFileStream>();
        await Assert.That(args.Stream.CanSeek).IsTrue();
        await Assert.That(args.Stream.Length).IsEqualTo(600_000_000);
    }

    [Test]
    public async Task OpenStreamAsync_WhenStreamDisposed_ReleasesFile()
    {
        // Arrange
        var cut = RenderStreamedArea();
        var file = await OpenStreamAsync(cut.Instance.OpenStreamAsync, size: 10);

        // Act
        await Streamed.Single().DisposeAsync();

        // Assert
        await Assert.That(file.Disposed).IsTrue();
    }

    [Test]
    public async Task OpenStreamAsync_OverMaxFileSize_ReportsTooLargeAndReleasesFile()
    {
        // Arrange
        var cut = RenderStreamedArea(maxFileSize: 10);

        // Act
        var file = await OpenStreamAsync(cut.Instance.OpenStreamAsync, size: 11);

        // Assert
        await Assert.That(Streamed.Count).IsEqualTo(0);
        await Assert.That(TooLarge.Single().Size).IsEqualTo(11);
        await Assert.That(file.Disposed).IsTrue();
    }

    [Test]
    [Arguments(10L, 10L)]
    [Arguments(0L, 1000L)]
    public async Task OpenStreamAsync_WithinMaxFileSize_InvokesOnStream(long maxFileSize, long size)
    {
        // Arrange
        var cut = RenderStreamedArea(maxFileSize: maxFileSize);

        // Act
        await OpenStreamAsync(cut.Instance.OpenStreamAsync, size);

        // Assert
        await Assert.That(Streamed.Count).IsEqualTo(1);
        await Assert.That(TooLarge.Count).IsEqualTo(0);
    }

    [Test]
    public async Task OpenStreamAsync_WithNegativeSize_ReleasesFile()
    {
        // Arrange
        var cut = RenderStreamedArea();

        // Act
        var file = await OpenStreamAsync(cut.Instance.OpenStreamAsync, size: -1);

        // Assert
        await Assert.That(Streamed.Count).IsEqualTo(0);
        await Assert.That(file.Disposed).IsTrue();
    }

    [Test]
    [Arguments(10L, 11, false)]
    [Arguments(10L, 10, true)]
    [Arguments(null, 1000, true)]
    [Arguments(0L, 1000, true)]
    public async Task UploadAsync_WithMaxFileSize_EnforcesLimit(long? maxFileSize, int size, bool accepted)
    {
        // Arrange
        var cut = RenderUploadArea(maxFileSize);

        // Act
        await UploadFileAsync(cut.Instance.UploadAsync, size);

        // Assert
        await ShouldBeAcceptedAsync(accepted);
    }

    [Test]
    public async Task UploadAsync_WithTooLargeFile_ReportsActualSize()
    {
        // Arrange
        var cut = RenderUploadArea(maxFileSize: 10);

        // Act
        await UploadFileAsync(cut.Instance.UploadAsync, size: 11);

        // Assert
        await Assert.That(TooLarge[0].Size).IsEqualTo(11);
    }

    [Test]
    public async Task Render_ByDefault_LeavesFolderAndPasteOff()
    {
        // Act
        var cut = Render<ReuUploadArea>();

        // Assert
        var input = cut.Find("input");
        await Assert.That(input.HasAttribute("webkitdirectory")).IsFalse();
        await Assert.That(input.HasAttribute("data-reu-paste")).IsFalse();
    }

    [Test]
    public async Task Render_WithDirectory_PicksFolder()
    {
        // Act
        var cut = Render<ReuUploadArea>(parameters => parameters.Add(x => x.Directory, true));

        // Assert
        await Assert.That(cut.Find("input").HasAttribute("webkitdirectory")).IsTrue();
    }

    [Test]
    public async Task Render_WithPaste_MarksInputForPaste()
    {
        // Act
        var cut = Render<ReuUploadArea>(parameters => parameters.Add(x => x.Paste, true));

        // Assert
        await Assert.That(cut.Find("input").HasAttribute("data-reu-paste")).IsTrue();
    }

    [Test]
    public async Task UploadAsync_WithRelativePath_PassesItOn()
    {
        // Arrange
        UploadEventArgs? uploaded = null;
        var cut = Render<ReuUploadArea>(parameters => parameters
            .Add(x => x.OnUpload, EventCallback.Factory.Create<UploadEventArgs>(this, x => uploaded = x)));

        // Act
        await cut.InvokeAsync(() => cut.Instance.UploadAsync("A01.Map.Gbx", [1], 0, "", "Maps/A01.Map.Gbx"));

        // Assert
        await Assert.That(uploaded!.FileName).IsEqualTo("A01.Map.Gbx");
        await Assert.That(uploaded.RelativePath).IsEqualTo("Maps/A01.Map.Gbx");
    }

    [Test]
    public async Task UploadAsync_WithoutRelativePath_LeavesItNull()
    {
        // Arrange
        UploadEventArgs? uploaded = null;
        var cut = Render<ReuUploadArea>(parameters => parameters
            .Add(x => x.OnUpload, EventCallback.Factory.Create<UploadEventArgs>(this, x => uploaded = x)));

        // Act
        await cut.InvokeAsync(() => UploadFileAsync(cut.Instance.UploadAsync, size: 1));

        // Assert
        await Assert.That(uploaded!.RelativePath).IsNull();
    }

    [Test]
    public async Task OpenStreamAsync_WithRelativePath_PassesItOn()
    {
        // Arrange
        var cut = Render<ReuUploadArea>(parameters => parameters
            .Add(x => x.Streamed, true)
            .Add(x => x.OnStream, OnStream));

        // Act
        await cut.InvokeAsync(() => cut.Instance.OpenStreamAsync(new FakeJSObjectReference(), "A01.Map.Gbx", 1, 0, "", "Maps/A01.Map.Gbx"));

        // Assert
        await Assert.That(Streamed.Single().RelativePath).IsEqualTo("Maps/A01.Map.Gbx");
    }

    [Test]
    public async Task UploadAsync_TooLargeWithRelativePath_ReportsIt()
    {
        // Arrange
        var cut = Render<ReuUploadArea>(parameters => parameters
            .Add(x => x.MaxFileSize, 1L)
            .Add(x => x.OnFileTooLarge, OnFileTooLarge));

        // Act
        await cut.InvokeAsync(() => cut.Instance.UploadAsync("A01.Map.Gbx", [1, 2], 0, "", "Maps/A01.Map.Gbx"));

        // Assert
        await Assert.That(TooLarge.Single().RelativePath).IsEqualTo("Maps/A01.Map.Gbx");
    }

    [Test]
    public async Task OnFileExceedCountAsync_WithRelativePath_ReportsIt()
    {
        // Arrange
        var cut = Render<ReuUploadArea>(parameters => parameters.Add(x => x.OnFileExceedCount, OnFileExceedCount));

        // Act
        await cut.InvokeAsync(() => cut.Instance.OnFileExceedCountAsync("A01.Map.Gbx", 1, 0, "", "Maps/A01.Map.Gbx"));

        // Assert
        await Assert.That(ExceededCount.Single().RelativePath).IsEqualTo("Maps/A01.Map.Gbx");
    }

    [Test]
    public async Task DisposeAsync_WhenRendered_RemovesPasteHandler()
    {
        // Arrange
        AreaModule.SetupVoid("removeHandlers", _ => true).SetVoidResult();
        Render<ReuUploadArea>();

        // Act
        await DisposeComponentsAsync();

        // Assert
        await Assert.That(AreaModule.Invocations["removeHandlers"].Count).IsEqualTo(1);
    }

    [Test]
    public async Task Render_WithContent_LabelsInputWithIt()
    {
        // Act
        var cut = Render<ReuUploadArea>(parameters => parameters.AddChildContent("Drop files here"));

        // Assert
        var label = cut.Find("label.upload-area");
        await Assert.That(label.QuerySelector("input[type=file]")).IsNotNull();
        await Assert.That(cut.Find("input").HasAttribute("aria-label")).IsFalse();
    }

    [Test]
    public async Task Render_WithoutContent_NamesInput()
    {
        // Act
        var cut = Render<ReuUploadArea>();

        // Assert
        await Assert.That(cut.Find("input").GetAttribute("aria-label")).IsEqualTo("Choose files");
    }
}
