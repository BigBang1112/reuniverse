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

                        const size = args[1] instanceof Uint8Array ? args[1].length : args[1];
                        window.calls.push(`${name} ${args[0]} ${size}`);
                    }
                };

                addHandlers(document.getElementById("input"), helper, {{(streamed ? "true" : "false")}});
            }
            """);
    }

    private async Task SelectAsync(params (string Name, int Size)[] files)
    {
        var payloads = files.Select(file => new FilePayload
        {
            Name = file.Name,
            MimeType = "application/octet-stream",
            Buffer = new byte[file.Size]
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
    public async Task Select_WhenStreamed_LeavesUploadToBlazor(string script)
    {
        // Arrange
        await OpenAsync(script, streamed: true);

        // Act
        await SelectAsync(("first.bin", 3));

        // Assert
        await Assert.That((await GetCallsAsync()).Length).IsEqualTo(0);
    }

    [Test]
    [Arguments(Area)]
    [Arguments(Button)]
    public async Task Select_WhenStreamed_DoesNotAskForLimits(string script)
    {
        // Arrange
        await OpenAsync(script, streamed: true);

        // Act
        await SelectAsync(("first.bin", 3));

        // Assert
        await Assert.That(await Page.EvaluateAsync<int>("window.limitQueries")).IsEqualTo(0);
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
