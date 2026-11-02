using Microsoft.AspNetCore.Components.Forms;

namespace Reuniverse.Razor.Tests;

public class ReuUploadScreenTests : ReuUploadTestBase
{
    [Test]
    public async Task Render_ByDefault_HidesScreen()
    {
        // Act
        var cut = Render<ReuUploadScreen>();

        // Assert
        await Assert.That(cut.Find(".upload-screen").ClassList.Contains("hidden")).IsTrue();
    }

    [Test]
    public async Task SetVisible_WithTrue_ShowsScreen()
    {
        // Arrange
        var cut = Render<ReuUploadScreen>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.SetVisible(true));

        // Assert
        await Assert.That(cut.Find(".upload-screen").ClassList.Contains("hidden")).IsFalse();
    }

    [Test]
    public async Task SetVisible_WithChange_InvokesIsVisibleChanged()
    {
        // Arrange
        bool? visible = null;
        var cut = Render<ReuUploadScreen>(parameters => parameters.Add(x => x.IsVisibleChanged, value => visible = value));

        // Act
        await cut.InvokeAsync(() => cut.Instance.SetVisible(true));

        // Assert
        await Assert.That(visible).IsTrue();
    }

    [Test]
    public async Task SetVisible_WithoutChange_DoesNotInvokeIsVisibleChanged()
    {
        // Arrange
        bool? visible = null;
        var cut = Render<ReuUploadScreen>(parameters => parameters.Add(x => x.IsVisibleChanged, value => visible = value));

        // Act
        await cut.InvokeAsync(() => cut.Instance.SetVisible(false));

        // Assert
        await Assert.That(visible).IsNull();
    }

    [Test]
    public async Task UploadAsync_WithAcceptedFile_HidesScreenAndForwardsFile()
    {
        // Arrange
        var cut = Render<ReuUploadScreen>(parameters => parameters
            .Add(x => x.IsVisible, true)
            .Add(x => x.OnUpload, OnUpload));
        var area = cut.FindComponent<ReuUploadArea>();

        // Act
        await cut.InvokeAsync(() => UploadFileAsync(area.Instance.UploadAsync, size: 1));

        // Assert
        await Assert.That(cut.Find(".upload-screen").ClassList.Contains("hidden")).IsTrue();
        await ShouldBeAcceptedAsync(accepted: true);
    }

    [Test]
    public async Task SelectFiles_WhenStreamed_HidesScreenAndForwardsStream()
    {
        // Arrange
        var cut = Render<ReuUploadScreen>(parameters => parameters
            .Add(x => x.IsVisible, true)
            .Add(x => x.Streamed, true)
            .Add(x => x.OnStream, OnStream));

        // Act
        cut.FindComponent<InputFile>().UploadFiles(CreateFile("file.txt"));

        // Assert
        await Assert.That(cut.Find(".upload-screen").ClassList.Contains("hidden")).IsTrue();
        await Streamed.ShouldEqualInOrderAsync("file.txt");
    }

    [Test]
    public async Task DisposeAsync_WhenRendered_RemovesDocumentHandlers()
    {
        // Arrange
        ScreenModule.SetupVoid("removeHandlers").SetVoidResult();
        Render<ReuUploadScreen>();

        // Act
        await DisposeComponentsAsync();

        // Assert
        await Assert.That(ScreenModule.Invocations["removeHandlers"].Count).IsEqualTo(1);
    }

    [Test]
    public async Task UploadAsync_WithTooLargeFile_SkipsOnUpload()
    {
        // Arrange
        var cut = Render<ReuUploadScreen>(parameters => parameters
            .Add(x => x.MaxFileSize, 10L)
            .Add(x => x.OnUpload, OnUpload)
            .Add(x => x.OnFileTooLarge, OnFileTooLarge));
        var area = cut.FindComponent<ReuUploadArea>();

        // Act
        await UploadFileAsync(area.Instance.UploadAsync, size: 11);

        // Assert
        await ShouldBeAcceptedAsync(accepted: false);
    }
}
