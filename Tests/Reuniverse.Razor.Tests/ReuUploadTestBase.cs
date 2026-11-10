using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Reuniverse.Razor.Tests;

public abstract class ReuUploadTestBase : BunitContext
{
    protected ReuUploadTestBase()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        AreaModule = JSInterop.SetupModule("./_content/Reuniverse.Razor/ReuUploadArea.razor.js");
        ButtonModule = JSInterop.SetupModule("./_content/Reuniverse.Razor/ReuUploadButton.razor.js");
        ScreenModule = JSInterop.SetupModule("./_content/Reuniverse.Razor/ReuUploadScreen.razor.js");
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    protected BunitJSModuleInterop AreaModule { get; }
    protected BunitJSModuleInterop ButtonModule { get; }
    protected BunitJSModuleInterop ScreenModule { get; }

    protected List<string> Uploaded { get; } = [];
    protected List<StreamEventArgs> Streamed { get; } = [];
    protected List<FileInfoEventArgs> TooLarge { get; } = [];
    protected List<FileInfoEventArgs> ExceededCount { get; } = [];

    protected EventCallback<UploadEventArgs> OnUpload => EventCallback.Factory.Create<UploadEventArgs>(this, x => Uploaded.Add(x.FileName));
    protected EventCallback<FileInfoEventArgs> OnFileTooLarge => EventCallback.Factory.Create<FileInfoEventArgs>(this, TooLarge.Add);
    protected EventCallback<FileInfoEventArgs> OnFileExceedCount => EventCallback.Factory.Create<FileInfoEventArgs>(this, ExceededCount.Add);
    protected EventCallback<StreamEventArgs> OnStream => EventCallback.Factory.Create<StreamEventArgs>(this, Streamed.Add);

    protected static Task UploadFileAsync(Func<string, byte[], long, string, string?, Task> upload, int size)
    {
        return upload("file.bin", new byte[size], 0, "application/octet-stream", null);
    }

    protected static async Task<FakeJSObjectReference> OpenStreamAsync(Func<IJSObjectReference, string, long, long, string, string?, Task> open, long size)
    {
        var file = new FakeJSObjectReference();
        await open(file, "file.bin", size, 0, "application/octet-stream", null);
        return file;
    }

    protected async Task ShouldBeAcceptedAsync(bool accepted)
    {
        await Assert.That(Uploaded.Count).IsEqualTo(accepted ? 1 : 0);
        await Assert.That(TooLarge.Count).IsEqualTo(accepted ? 0 : 1);
    }
}
