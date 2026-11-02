using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

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

    private IRenderedComponent<ReuUploadArea> RenderStreamedArea(long? maxFileSize = null, int? maxFileCount = null)
    {
        return Render<ReuUploadArea>(parameters => parameters
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
        await cut.Instance.OnFileTooLargeAsync("file.bin", 11, 0, "application/octet-stream");

        // Assert
        await Assert.That(TooLarge[0].FileName).IsEqualTo("file.bin");
    }

    [Test]
    public async Task OnFileExceedCountAsync_FromScript_InvokesCallback()
    {
        // Arrange
        var cut = Render<ReuUploadArea>(parameters => parameters.Add(x => x.OnFileExceedCount, OnFileExceedCount));

        // Act
        await cut.Instance.OnFileExceedCountAsync("file.bin", 11, 0, "application/octet-stream");

        // Assert
        await Assert.That(ExceededCount[0].FileName).IsEqualTo("file.bin");
    }

    [Test]
    public async Task SelectFiles_WhenStreamed_InvokesOnStream()
    {
        // Arrange
        var cut = RenderStreamedArea();

        // Act
        cut.FindComponent<InputFile>().UploadFiles(CreateFile("first.txt"), CreateFile("second.txt"));

        // Assert
        await Streamed.ShouldEqualInOrderAsync("first.txt", "second.txt");
    }

    [Test]
    public async Task SelectFiles_WhenStreamedOverMaxFileSize_ReportsTooLarge()
    {
        // Arrange
        var cut = RenderStreamedArea(maxFileSize: 3);

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
        var cut = RenderStreamedArea(maxFileCount: 1);

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
}
