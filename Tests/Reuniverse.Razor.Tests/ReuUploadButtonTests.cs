using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

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

    private IRenderedComponent<ReuUploadButton> RenderStreamedButton(long? maxFileSize = null, int? maxFileCount = null)
    {
        return Render<ReuUploadButton>(parameters => parameters
            .Add(x => x.Streamed, true)
            .Add(x => x.MaxFileSize, maxFileSize)
            .Add(x => x.MaxFileCount, maxFileCount)
            .Add(x => x.OnStream, OnStream)
            .Add(x => x.OnFileTooLarge, OnFileTooLarge)
            .Add(x => x.OnFileExceedCount, OnFileExceedCount));
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
    public async Task SelectFiles_WhenStreamed_InvokesOnStream()
    {
        // Arrange
        var cut = RenderStreamedButton();

        // Act
        cut.FindComponent<InputFile>().UploadFiles(CreateFile("first.txt"), CreateFile("second.txt"));

        // Assert
        await Streamed.ShouldEqualInOrderAsync("first.txt", "second.txt");
    }

    [Test]
    public async Task SelectFiles_WhenStreamedOverMaxFileSize_ReportsTooLarge()
    {
        // Arrange
        var cut = RenderStreamedButton(maxFileSize: 3);

        // Act
        cut.FindComponent<InputFile>().UploadFiles(CreateFile("large.txt", "too long"));

        // Assert
        await Assert.That(Streamed.Count).IsEqualTo(0);
        await Assert.That(TooLarge.Count).IsEqualTo(1);
    }

    [Test]
    public async Task SelectFiles_WhenStreamedOverMaxFileCount_ReportsExtraFiles()
    {
        // Arrange
        var cut = RenderStreamedButton(maxFileCount: 1);

        // Act
        cut.FindComponent<InputFile>().UploadFiles(CreateFile("first.txt"), CreateFile("second.txt"));

        // Assert
        await Streamed.ShouldEqualInOrderAsync("first.txt");
        await ExceededCount.Select(x => x.FileName).ShouldEqualInOrderAsync("second.txt");
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
}
