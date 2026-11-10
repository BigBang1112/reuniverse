using Microsoft.Playwright;

namespace Reuniverse.Razor.Tests.JavaScript;

// both components share this script, so everything runs against both copies
public class UploadScriptTests : BrowserTestBase
{
    private const string Area = "ReuUploadArea.razor.js";
    private const string Button = "ReuUploadButton.razor.js";

    private async Task OpenAsync(string script, int maxFileCount = 0, long maxFileSize = 0, bool streamed = false)
    {
        await GoToAsync("""<input id="input" type="file" multiple />""");

        await Page.EvaluateAsync($$"""
            async () => {
                const { addHandlers } = await import("/{{script}}");

                window.calls = [];
                window.limitQueries = 0;
                window.streamedFiles = [];

                // Blazor's global, which hands the object over as is here
                window.DotNet = { createJSObjectReference: obj => obj };

                const helper = {
                    invokeMethodAsync: async (name, ...args) => {
                        if (name === "GetMaxFileCountAsync") {
                            window.limitQueries++;
                            return {{maxFileCount}};
                        }

                        if (name === "GetMaxFileSizeAsync") {
                            window.limitQueries++;
                            return {{maxFileSize}};
                        }

                        if (name === "OpenStreamAsync") {
                            window.streamedFiles.push(args[0]);
                            window.calls.push(`${name} ${args[1]} ${args[2]}`);
                            return;
                        }

                        const size = args[1] instanceof Uint8Array ? args[1].length : args[1];
                        window.calls.push(`${name} ${args[0]} ${size}`);
                    }
                };

                addHandlers(document.getElementById("input"), helper, {{(streamed ? "true" : "false")}});
            }
            """);
    }

    private Task SelectAsync(params (string Name, int Size)[] files)
    {
        return SelectAsync(files.Select(file => (file.Name, new byte[file.Size])).ToArray());
    }

    private async Task SelectAsync(params (string Name, byte[] Data)[] files)
    {
        var payloads = files.Select(file => new FilePayload
        {
            Name = file.Name,
            MimeType = "application/octet-stream",
            Buffer = file.Data
        });

        await Page.Locator("#input").SetInputFilesAsync(payloads);

        // the script clears the input once it is done
        await Page.WaitForFunctionAsync("""document.getElementById("input").files.length === 0""");
    }

    private async Task<string[]> GetCallsAsync()
    {
        return await Page.EvaluateAsync<string[]>("window.calls");
    }

    [Test]
    [Arguments(Area)]
    [Arguments(Button)]
    public async Task Select_WithoutLimits_UploadsFiles(string script)
    {
        // Arrange
        await OpenAsync(script);

        // Act
        await SelectAsync(("first.bin", 3), ("second.bin", 5));

        // Assert
        await (await GetCallsAsync()).ShouldEqualInOrderAsync("UploadAsync first.bin 3", "UploadAsync second.bin 5");
    }

    [Test]
    [Arguments(Area)]
    [Arguments(Button)]
    public async Task Select_OverMaxFileCount_ReportsExtraFiles(string script)
    {
        // Arrange
        await OpenAsync(script, maxFileCount: 1);

        // Act
        await SelectAsync(("first.bin", 3), ("second.bin", 5));

        // Assert
        await (await GetCallsAsync()).ShouldEqualInOrderAsync("UploadAsync first.bin 3", "OnFileExceedCountAsync second.bin 5");
    }

    [Test]
    [Arguments(Area)]
    [Arguments(Button)]
    public async Task Select_OverMaxFileSize_ReportsTooLarge(string script)
    {
        // Arrange
        await OpenAsync(script, maxFileSize: 4);

        // Act
        await SelectAsync(("small.bin", 4), ("large.bin", 5));

        // Assert
        await (await GetCallsAsync()).ShouldEqualInOrderAsync("UploadAsync small.bin 4", "OnFileTooLargeAsync large.bin 5");
    }

    [Test]
    [Arguments(Area)]
    [Arguments(Button)]
    public async Task Select_OverBothLimits_ReportsCountFirst(string script)
    {
        // Arrange
        await OpenAsync(script, maxFileCount: 1, maxFileSize: 4);

        // Act
        await SelectAsync(("first.bin", 1), ("second.bin", 9));

        // Assert
        await (await GetCallsAsync()).ShouldEqualInOrderAsync("UploadAsync first.bin 1", "OnFileExceedCountAsync second.bin 9");
    }

    [Test]
    [Arguments(Area)]
    [Arguments(Button)]
    public async Task Select_WhenStreamed_OpensStreamsInsteadOfUploading(string script)
    {
        // Arrange
        await OpenAsync(script, streamed: true);

        // Act
        await SelectAsync(("first.bin", 3), ("second.bin", 5));

        // Assert
        await (await GetCallsAsync()).ShouldEqualInOrderAsync("OpenStreamAsync first.bin 3", "OpenStreamAsync second.bin 5");
    }

    [Test]
    [Arguments(Area)]
    [Arguments(Button)]
    public async Task Select_WhenStreamedOverLimits_ReportsThem(string script)
    {
        // Arrange
        await OpenAsync(script, maxFileCount: 2, maxFileSize: 4, streamed: true);

        // Act
        await SelectAsync(("small.bin", 4), ("large.bin", 5), ("extra.bin", 1));

        // Assert
        await (await GetCallsAsync()).ShouldEqualInOrderAsync(
            "OpenStreamAsync small.bin 4",
            "OnFileTooLargeAsync large.bin 5",
            "OnFileExceedCountAsync extra.bin 1");
    }

    [Test]
    [Arguments(Area)]
    [Arguments(Button)]
    public async Task Select_WhenStreamed_ReadsRequestedRange(string script)
    {
        // Arrange
        await OpenAsync(script, streamed: true);
        await SelectAsync(("file.bin", new byte[] { 10, 11, 12, 13, 14 }));

        // Act
        var result = await Page.EvaluateAsync<int[]>("async () => Array.from(await window.streamedFiles[0].read(1, 3))");

        // Assert
        await result.ShouldEqualInOrderAsync(11, 12, 13);
    }

    [Test]
    [Arguments(Area)]
    [Arguments(Button)]
    public async Task Select_WhenStreamed_ReadPastEndReturnsRest(string script)
    {
        // Arrange
        await OpenAsync(script, streamed: true);
        await SelectAsync(("file.bin", new byte[] { 10, 11, 12, 13, 14 }));

        // Act
        var result = await Page.EvaluateAsync<int[]>("async () => Array.from(await window.streamedFiles[0].read(3, 10))");

        // Assert
        await result.ShouldEqualInOrderAsync(13, 14);
    }

    [Test]
    [Arguments(Area)]
    [Arguments(Button)]
    public async Task Select_WhenStreamed_SlicesRangeAsBlob(string script)
    {
        // Arrange
        await OpenAsync(script, streamed: true);
        await SelectAsync(("file.bin", new byte[] { 10, 11, 12, 13, 14 }));

        // Act
        var result = await Page.EvaluateAsync<int[]>("""
            async () => {
                const blob = window.streamedFiles[0].slice(2, 2);
                return blob instanceof Blob ? Array.from(new Uint8Array(await blob.arrayBuffer())) : [];
            }
            """);

        // Assert
        await result.ShouldEqualInOrderAsync(12, 13);
    }

    [Test]
    [Arguments(Area)]
    [Arguments(Button)]
    public async Task Select_SameFileTwice_UploadsItAgain(string script)
    {
        // Arrange
        await OpenAsync(script);
        await SelectAsync(("first.bin", 3));

        // Act
        await SelectAsync(("first.bin", 3));

        // Assert
        await Assert.That((await GetCallsAsync()).Length).IsEqualTo(2);
    }

    [Test]
    public async Task Scripts_OfBothComponents_StayIdentical()
    {
        // Arrange
        var directory = Path.Combine(AppContext.BaseDirectory, "Scripts");

        // Act
        var area = await File.ReadAllTextAsync(Path.Combine(directory, Area));
        var button = await File.ReadAllTextAsync(Path.Combine(directory, Button));

        // Assert
        await Assert.That(area).IsEqualTo(button);
    }
}
