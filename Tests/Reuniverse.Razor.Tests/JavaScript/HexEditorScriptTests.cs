namespace Reuniverse.Razor.Tests.JavaScript;

public class HexEditorScriptTests : BrowserTestBase
{
    private const string Script = "ReuHexEditor.razor.js";

    // a header and 100 rows of 20 pixels in a box 200 pixels high, like the editor lays them out
    private const string Body = """
        <div id="editor" tabindex="0" style="height: 200px; overflow: auto">
            <div class="hex-header" style="height: 20px; position: sticky; top: 0">Header</div>
            <div style="height: 2000px"></div>
        </div>
        <button id="other">Other</button>
        <script>
            window.addEventListener("keydown", event => window.lastKeyPrevented = event.defaultPrevented);
        </script>
        """;

    private async Task OpenAsync()
    {
        await GoToAsync(Body);
        await Page.EvaluateAsync($$"""
            async () => {
                window.hex = await import("/{{Script}}");
                window.hex.init(document.getElementById("editor"));
            }
            """);
    }

    private async Task<bool> PressAsync(string selector, string key)
    {
        await Page.Locator(selector).FocusAsync();
        await Page.Keyboard.PressAsync(key);
        return await Page.EvaluateAsync<bool>("window.lastKeyPrevented");
    }

    private async Task<double> ScrollToRowAsync(int row)
    {
        return await Page.EvaluateAsync<double>($$"""
            () => {
                const editor = document.getElementById("editor");
                window.hex.scrollToRow(editor, {{row}}, 20);
                return editor.scrollTop;
            }
            """);
    }

    [Test]
    [Arguments("ArrowDown")]
    [Arguments("PageDown")]
    [Arguments("End")]
    [Arguments("Space")]
    [Arguments("a")]
    [Arguments("Control+a")]
    public async Task KeyDown_HandledKeyOnEditor_IsCancelled(string key)
    {
        // Arrange
        await OpenAsync();

        // Act
        var prevented = await PressAsync("#editor", key);

        // Assert
        await Assert.That(prevented).IsTrue();
    }

    [Test]
    [Arguments("Tab")]
    [Arguments("Control+c")]
    [Arguments("F5")]
    public async Task KeyDown_OtherKeyOnEditor_IsLeftAlone(string key)
    {
        // Arrange
        await OpenAsync();

        // Act
        var prevented = await PressAsync("#editor", key);

        // Assert
        await Assert.That(prevented).IsFalse();
    }

    [Test]
    public async Task KeyDown_OutsideEditor_IsLeftAlone()
    {
        // Arrange
        await OpenAsync();

        // Act
        var prevented = await PressAsync("#other", "ArrowDown");

        // Assert
        await Assert.That(prevented).IsFalse();
    }

    [Test]
    public async Task KeyDown_AfterDispose_IsLeftAlone()
    {
        // Arrange
        await OpenAsync();
        await Page.EvaluateAsync("""window.hex.dispose(document.getElementById("editor"))""");

        // Act
        var prevented = await PressAsync("#editor", "ArrowDown");

        // Assert
        await Assert.That(prevented).IsFalse();
    }

    [Test]
    public async Task ScrollToRow_BelowView_ScrollsUntilRowIsAtBottom()
    {
        // Arrange
        await OpenAsync();

        // Act
        var scrollTop = await ScrollToRowAsync(20);

        // Assert
        // the row ends at 20 + 21 * 20 = 440, and the box is 200 high
        await Assert.That(scrollTop).IsEqualTo(240);
    }

    [Test]
    public async Task ScrollToRow_AboveView_ScrollsUntilRowIsBelowHeader()
    {
        // Arrange
        await OpenAsync();
        await ScrollToRowAsync(50);

        // Act
        var scrollTop = await ScrollToRowAsync(10);

        // Assert
        await Assert.That(scrollTop).IsEqualTo(200);
    }

    [Test]
    public async Task ScrollToRow_AlreadyVisible_DoesNotScroll()
    {
        // Arrange
        await OpenAsync();

        // Act
        var scrollTop = await ScrollToRowAsync(3);

        // Assert
        await Assert.That(scrollTop).IsEqualTo(0);
    }

    // the same structure as the editor renders, with the cursor's digits in spans of their own
    private const string RowsBody = """
        <div id="editor" tabindex="0">
            <div class="hex-row hex-header"><span class="offset">Offset</span><span class="hex"><span class="b" id="h0">00</span><span class="b" id="h1">01</span></span><span class="text">Text</span></div>
            <div class="hex-row"><span class="offset">00000000</span><span class="hex"><span class="b" id="x0"><span class="nibble" id="n0">4</span><span class="nibble">1</span></span><span class="b" id="x1">42</span></span><span class="text"><span class="b" id="t0">A</span><span class="b" id="t1">B</span></span></div>
        </div>
        <div id="outside" style="height: 50px">Outside</div>
        """;

    private async Task OpenRowsAsync()
    {
        await GoToAsync(RowsBody);
        await Page.EvaluateAsync($$"""
            async () => {
                window.hex = await import("/{{Script}}");
                window.hex.init(document.getElementById("editor"));
            }
            """);
    }

    private Task<string[]> GetHoveredAsync()
    {
        return Page.EvaluateAsync<string[]>("""[...document.querySelectorAll("[data-reu-hover]")].map(x => x.id)""");
    }

    [Test]
    public async Task Hover_HexByte_MarksItsTextAndColumn()
    {
        // Arrange
        await OpenRowsAsync();

        // Act
        await Page.Locator("#x1").HoverAsync();

        // Assert
        await (await GetHoveredAsync()).Order().ShouldEqualInOrderAsync("h1", "t1", "x1");
    }

    [Test]
    public async Task Hover_TextByte_MarksItsHexAndColumn()
    {
        // Arrange
        await OpenRowsAsync();

        // Act
        await Page.Locator("#t0").HoverAsync();

        // Assert
        await (await GetHoveredAsync()).Order().ShouldEqualInOrderAsync("h0", "t0", "x0");
    }

    [Test]
    public async Task Hover_DigitOfCursor_MarksItsByte()
    {
        // Arrange
        await OpenRowsAsync();

        // Act
        await Page.Locator("#n0").HoverAsync();

        // Assert
        await (await GetHoveredAsync()).Order().ShouldEqualInOrderAsync("h0", "t0", "x0");
    }

    [Test]
    public async Task Hover_AnotherByte_MovesMarks()
    {
        // Arrange
        await OpenRowsAsync();
        await Page.Locator("#x0").HoverAsync();

        // Act
        await Page.Locator("#t1").HoverAsync();

        // Assert
        await (await GetHoveredAsync()).Order().ShouldEqualInOrderAsync("h1", "t1", "x1");
    }

    [Test]
    public async Task Hover_HeaderOrOffset_MarksNothing()
    {
        // Arrange
        await OpenRowsAsync();
        await Page.Locator("#x0").HoverAsync();

        // Act
        await Page.Locator("#h1").HoverAsync();

        // Assert
        await Assert.That((await GetHoveredAsync()).Length).IsEqualTo(0);
    }

    [Test]
    public async Task MouseLeave_Editor_ClearsMarks()
    {
        // Arrange
        await OpenRowsAsync();
        await Page.Locator("#x0").HoverAsync();

        // Act
        await Page.Locator("#outside").HoverAsync();

        // Assert
        await Assert.That((await GetHoveredAsync()).Length).IsEqualTo(0);
    }

    [Test]
    public async Task Dispose_WhileHovering_ClearsMarksAndStops()
    {
        // Arrange
        await OpenRowsAsync();
        await Page.Locator("#x0").HoverAsync();

        // Act
        await Page.EvaluateAsync("""window.hex.dispose(document.getElementById("editor"))""");
        await Page.Locator("#x1").HoverAsync();

        // Assert
        await Assert.That((await GetHoveredAsync()).Length).IsEqualTo(0);
    }
}
