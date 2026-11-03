namespace Reuniverse.Razor.Tests.JavaScript;

public class TabKeysScriptTests : BrowserTestBase
{
    private const string Script = """<script src="/reuniverse.js" defer></script>""";

    // the same structure as ReuTabs renders, and a record of whether the last key was cancelled by the time it reached the window
    private const string Body = """
        <div role="tablist">
            <div class="tab"><button id="tab" role="tab">One</button><button id="close">✕</button></div>
        </div>
        <button id="plain">Plain</button>
        <script>
            window.addEventListener("keydown", event => window.lastKeyPrevented = event.defaultPrevented);
        </script>
        """;

    private async Task<bool> PressAsync(string selector, string key)
    {
        await GoToAsync(Body, Script);
        await Page.FocusAsync(selector);
        await Page.Keyboard.PressAsync(key);
        return await Page.EvaluateAsync<bool>("() => window.lastKeyPrevented");
    }

    [Test]
    [Arguments("ArrowLeft")]
    [Arguments("ArrowRight")]
    [Arguments("Home")]
    [Arguments("End")]
    public async Task KeyDown_OnTab_CancelsScrolling(string key)
    {
        // Act
        var prevented = await PressAsync("#tab", key);

        // Assert
        await Assert.That(prevented).IsTrue();
    }

    [Test]
    public async Task KeyDown_WithOtherKeyOnTab_LeavesKey()
    {
        // Act
        var prevented = await PressAsync("#tab", "ArrowDown");

        // Assert
        await Assert.That(prevented).IsFalse();
    }

    [Test]
    public async Task KeyDown_OnCloseButton_LeavesKey()
    {
        // Act
        var prevented = await PressAsync("#close", "Home");

        // Assert
        await Assert.That(prevented).IsFalse();
    }

    [Test]
    public async Task KeyDown_OutsideTabs_LeavesKey()
    {
        // Act
        var prevented = await PressAsync("#plain", "Home");

        // Assert
        await Assert.That(prevented).IsFalse();
    }
}
