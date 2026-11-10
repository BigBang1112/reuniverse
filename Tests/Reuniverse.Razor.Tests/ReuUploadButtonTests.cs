using Microsoft.AspNetCore.Components;

namespace Reuniverse.Razor.Tests;

public class ReuUploadButtonTests : ReuUploadTestBase
{
    private IRenderedComponent<ReuUploadButton> RenderUploadButton(long? maxFileSize)
    {
        return Render<ReuUploadButton>(parameters => parameters
            .Add(x => x.MaxFileSize, maxFileSize)
            .Add(x => x.OnUpload, OnUpload)
            .Add(x => x.OnFileTooLarge, OnFileTooLarge));
    }

    private IRenderedComponent<ReuUploadButton> RenderStreamedButton(long? maxFileSize = null)
    {
        return Render<ReuUploadButton>(parameters => parameters
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
        var cut = Render<ReuUploadButton>();

        // Assert
        await Assert.That(cut.FindAll(".loader").Count).IsEqualTo(1);
    }

    [Test]
    public async Task DragEnter_OverInput_ActivatesButton()
    {
        // Arrange
        var cut = Render<ReuUploadButton>();

        // Act
        cut.Find("input").DragEnter();

        // Assert
        await Assert.That(cut.Find("button").ClassList.Contains("active")).IsTrue();
    }

    [Test]
    public async Task DragLeave_AfterDragEnter_DeactivatesButton()
    {
        // Arrange
        var cut = Render<ReuUploadButton>();
        cut.Find("input").DragEnter();

        // Act
        cut.Find("input").DragLeave();

        // Assert
        await Assert.That(cut.Find("button").ClassList.Contains("active")).IsFalse();
    }

    [Test]
    public async Task OpenStreamAsync_WithFile_InvokesOnStreamWithSeekableStream()
    {
        // Arrange
        var cut = RenderStreamedButton();

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
        var cut = RenderStreamedButton();
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
        var cut = RenderStreamedButton(maxFileSize: 10);

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
        var cut = RenderStreamedButton(maxFileSize: maxFileSize);

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
        var cut = RenderStreamedButton();

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
        var cut = RenderUploadButton(maxFileSize);

        // Act
        await UploadFileAsync(cut.Instance.UploadAsync, size);

        // Assert
        await ShouldBeAcceptedAsync(accepted);
    }

    [Test]
    public async Task UploadAsync_WithTooLargeFile_ReportsActualSize()
    {
        // Arrange
        var cut = RenderUploadButton(maxFileSize: 10);

        // Act
        await UploadFileAsync(cut.Instance.UploadAsync, size: 11);

        // Assert
        await Assert.That(TooLarge[0].Size).IsEqualTo(11);
    }

    [Test]
    public async Task Render_ByDefault_LeavesFolderAndPasteOff()
    {
        // Act
        var cut = Render<ReuUploadButton>();

        // Assert
        var input = cut.Find("input");
        await Assert.That(input.HasAttribute("webkitdirectory")).IsFalse();
        await Assert.That(input.HasAttribute("data-reu-paste")).IsFalse();
    }

    [Test]
    public async Task Render_WithDirectory_PicksFolder()
    {
        // Act
        var cut = Render<ReuUploadButton>(parameters => parameters.Add(x => x.Directory, true));

        // Assert
        await Assert.That(cut.Find("input").HasAttribute("webkitdirectory")).IsTrue();
    }

    [Test]
    public async Task Render_WithPaste_MarksInputForPaste()
    {
        // Act
        var cut = Render<ReuUploadButton>(parameters => parameters.Add(x => x.Paste, true));

        // Assert
        await Assert.That(cut.Find("input").HasAttribute("data-reu-paste")).IsTrue();
    }

    [Test]
    public async Task UploadAsync_WithRelativePath_PassesItOn()
    {
        // Arrange
        UploadEventArgs? uploaded = null;
        var cut = Render<ReuUploadButton>(parameters => parameters
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
        var cut = Render<ReuUploadButton>(parameters => parameters
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
        var cut = Render<ReuUploadButton>(parameters => parameters
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
        var cut = Render<ReuUploadButton>(parameters => parameters
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
        var cut = Render<ReuUploadButton>(parameters => parameters.Add(x => x.OnFileExceedCount, OnFileExceedCount));

        // Act
        await cut.InvokeAsync(() => cut.Instance.OnFileExceedCountAsync("A01.Map.Gbx", 1, 0, "", "Maps/A01.Map.Gbx"));

        // Assert
        await Assert.That(ExceededCount.Single().RelativePath).IsEqualTo("Maps/A01.Map.Gbx");
    }

    [Test]
    public async Task DisposeAsync_WhenRendered_RemovesPasteHandler()
    {
        // Arrange
        ButtonModule.SetupVoid("removeHandlers", _ => true).SetVoidResult();
        Render<ReuUploadButton>();

        // Act
        await DisposeComponentsAsync();

        // Assert
        await Assert.That(ButtonModule.Invocations["removeHandlers"].Count).IsEqualTo(1);
    }
}
