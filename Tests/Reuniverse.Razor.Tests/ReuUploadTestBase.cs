using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Reuniverse.Razor.Tests;

public abstract class ReuUploadTestBase : BunitContext
{
    protected ReuUploadTestBase()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule("./_content/Reuniverse.Razor/ReuUploadArea.razor.js");
        JSInterop.SetupModule("./_content/Reuniverse.Razor/ReuUploadButton.razor.js");
        ScreenModule = JSInterop.SetupModule("./_content/Reuniverse.Razor/ReuUploadScreen.razor.js");
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    protected BunitJSModuleInterop ScreenModule { get; }

    protected List<string> Uploaded { get; } = [];
    protected List<string> Streamed { get; } = [];
    protected List<FileInfoEventArgs> TooLarge { get; } = [];
    protected List<FileInfoEventArgs> ExceededCount { get; } = [];

    protected EventCallback<UploadEventArgs> OnUpload => EventCallback.Factory.Create<UploadEventArgs>(this, x => Uploaded.Add(x.FileName));
    protected EventCallback<FileInfoEventArgs> OnFileTooLarge => EventCallback.Factory.Create<FileInfoEventArgs>(this, TooLarge.Add);
    protected EventCallback<FileInfoEventArgs> OnFileExceedCount => EventCallback.Factory.Create<FileInfoEventArgs>(this, ExceededCount.Add);
    protected EventCallback<StreamEventArgs> OnStream => EventCallback.Factory.Create<StreamEventArgs>(this, x => Streamed.Add(x.FileName));

    protected static InputFileContent CreateFile(string name, string content = "content") => InputFileContent.CreateFromText(content, name);

    protected static Task UploadFileAsync(Func<string, byte[], long, string, Task> upload, int size)
    {
        return upload("file.bin", new byte[size], 0, "application/octet-stream");
    }

    protected async Task ShouldBeAcceptedAsync(bool accepted)
    {
        await Assert.That(Uploaded.Count).IsEqualTo(accepted ? 1 : 0);
        await Assert.That(TooLarge.Count).IsEqualTo(accepted ? 0 : 1);
    }
}
