using Microsoft.Playwright;

namespace Reuniverse.Razor.Tests.JavaScript;

public class LogsScriptTests : BrowserTestBase
{
    [Test]
    public async Task ScrollToBottom_WithOverflowingContent_ScrollsToTheEnd()
    {
        // Arrange
        await GoToAsync("""<div id="list" style="height: 50px; overflow: auto;"><div style="height: 500px;"></div></div>""");

        // Act
        await Page.EvaluateAsync("""
            async () => {
                const { scrollToBottom } = await import("/ReuLogs.razor.js");
                scrollToBottom(document.getElementById("list"));
            }
            """);

        // Assert
        await Assert.That(await Page.EvaluateAsync<int>("""document.getElementById("list").scrollTop""")).IsEqualTo(450);
    }
}
