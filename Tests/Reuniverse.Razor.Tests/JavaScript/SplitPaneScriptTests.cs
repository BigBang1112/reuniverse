namespace Reuniverse.Razor.Tests.JavaScript;

public class SplitPaneScriptTests : BrowserTestBase
{
    private const string Head = """
        <link rel="stylesheet" href="/reuniverse.css" />
        <script src="/reuniverse.js" defer></script>
        """;

    // 600 pixels across, of which the handle takes 8, so the panes start at 300 and 292
    private static string Body(string style = "", string attributes = "", bool vertical = false) => $"""
        <div id="pane" class="reu-split-pane{(vertical ? " vertical" : "")}" data-reu-split-size="50%" {attributes}
             style="--reu-split-size: 50%; width: 600px; height: 600px; {style}">
            <div class="reu-split-first" id="first">First</div>
            <div class="reu-split-handle" id="handle" role="separator" tabindex="0" aria-valuemin="0" aria-valuemax="100"></div>
            <div class="reu-split-second" id="second">Second</div>
        </div>
        <script>
            window.resizes = [];
            document.addEventListener("reu-split-resize", event => window.resizes.push(event.detail.size));
        </script>
        """;

    private async Task OpenAsync(string style = "", string attributes = "", bool vertical = false)
    {
        await GoToAsync(Body(style, attributes, vertical), Head);
        await Page.WaitForFunctionAsync("""document.getElementById("handle").hasAttribute("aria-valuenow")""");
    }

    private async Task<double> GetFirstSizeAsync(bool vertical = false)
    {
        return await Page.EvaluateAsync<double>($"""
            document.getElementById("first").getBoundingClientRect().{(vertical ? "height" : "width")}
            """);
    }

    private Task<string?> GetValueNowAsync()
    {
        return Page.Locator("#handle").GetAttributeAsync("aria-valuenow");
    }

    private Task<string> GetSizeVariableAsync()
    {
        return Page.EvaluateAsync<string>("""document.getElementById("pane").style.getPropertyValue("--reu-split-size").trim()""");
    }

    private async Task DragAsync(double dx, double dy = 0)
    {
        var box = (await Page.Locator("#handle").BoundingBoxAsync())!;
        var x = box.X + box.Width / 2;
        var y = box.Y + box.Height / 2;

        await Page.Mouse.MoveAsync((float)x, (float)y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync((float)(x + dx), (float)(y + dy), new() { Steps = 5 });
        await Page.Mouse.UpAsync();
    }

    private async Task PressAsync(string key)
    {
        await Page.Locator("#handle").FocusAsync();
        await Page.Keyboard.PressAsync(key);
    }

    [Test]
    public async Task Load_SetsValueToFirstPaneShare()
    {
        // Act
        await OpenAsync();

        // Assert
        await Assert.That(await GetValueNowAsync()).IsEqualTo("50");
    }

    [Test]
    public async Task Drag_Handle_ResizesFirstPane()
    {
        // Arrange
        await OpenAsync();

        // Act
        await DragAsync(100);

        // Assert
        await Assert.That(await GetFirstSizeAsync()).IsEqualTo(400).Within(1);
        await Assert.That(await GetSizeVariableAsync()).IsEqualTo("66.67%");
        await Assert.That(await GetValueNowAsync()).IsEqualTo("67");
    }

    [Test]
    public async Task Drag_WithBorder_FollowsPointer()
    {
        // Arrange
        await GoToAsync(Body().Replace("class=\"reu-split-pane\"", "class=\"reu-split-pane bordered\""), Head);
        await Page.WaitForFunctionAsync("""document.getElementById("handle").hasAttribute("aria-valuenow")""");
        var before = await GetFirstSizeAsync();

        // Act
        await DragAsync(100);

        // Assert
        await Assert.That(await GetFirstSizeAsync()).IsEqualTo(before + 100).Within(1);
    }

    [Test]
    public async Task Drag_Handle_RaisesResizeEventOnce()
    {
        // Arrange
        await OpenAsync();

        // Act
        await DragAsync(-150);

        // Assert
        var resizes = await Page.EvaluateAsync<string[]>("window.resizes");
        await resizes.ShouldEqualInOrderAsync("25%");
    }

    [Test]
    public async Task Drag_PastFirstMinimum_StopsAtIt()
    {
        // Arrange
        await OpenAsync("--reu-split-first-min: 100px;");

        // Act
        await DragAsync(-500);

        // Assert
        await Assert.That(await GetFirstSizeAsync()).IsEqualTo(100).Within(1);
    }

    [Test]
    public async Task Drag_PastSecondMinimum_StopsAtIt()
    {
        // Arrange
        await OpenAsync("--reu-split-second-min: 100px;");

        // Act
        await DragAsync(500);

        // Assert
        await Assert.That(await GetFirstSizeAsync()).IsEqualTo(492).Within(1);
    }

    [Test]
    public async Task Drag_PastMinimum_KeepsSizeItStoppedAt()
    {
        // Arrange
        await OpenAsync("--reu-split-first-min: 120px;");

        // Act
        await DragAsync(-500);

        // Assert
        await Assert.That(await GetSizeVariableAsync()).IsEqualTo("20%");
    }

    [Test]
    public async Task Drag_Vertical_ResizesFirstPaneHeight()
    {
        // Arrange
        await OpenAsync(vertical: true);

        // Act
        await DragAsync(0, -100);

        // Assert
        await Assert.That(await GetFirstSizeAsync(vertical: true)).IsEqualTo(200).Within(1);
    }

    [Test]
    [Arguments("ArrowRight", 310)]
    [Arguments("ArrowLeft", 290)]
    [Arguments("Shift+ArrowRight", 350)]
    [Arguments("Home", 0)]
    [Arguments("End", 592)]
    public async Task KeyDown_OnHandle_ResizesFirstPane(string key, double expected)
    {
        // Arrange
        await OpenAsync();

        // Act
        await PressAsync(key);

        // Assert
        await Assert.That(await GetFirstSizeAsync()).IsEqualTo(expected).Within(1);
    }

    [Test]
    [Arguments("ArrowDown", 310)]
    [Arguments("ArrowUp", 290)]
    [Arguments("ArrowRight", 300)]
    public async Task KeyDown_OnVerticalHandle_UsesUpAndDown(string key, double expected)
    {
        // Arrange
        await OpenAsync(vertical: true);

        // Act
        await PressAsync(key);

        // Assert
        await Assert.That(await GetFirstSizeAsync(vertical: true)).IsEqualTo(expected).Within(1);
    }

    [Test]
    public async Task KeyDown_Arrow_KeepsPageFromScrolling()
    {
        // Arrange
        await OpenAsync();
        await Page.EvaluateAsync("""window.addEventListener("keydown", event => window.lastKeyPrevented = event.defaultPrevented)""");

        // Act
        await PressAsync("ArrowRight");

        // Assert
        await Assert.That(await Page.EvaluateAsync<bool>("window.lastKeyPrevented")).IsTrue();
    }

    [Test]
    public async Task DoubleClick_AfterResize_GoesBackToInitialSize()
    {
        // Arrange
        await OpenAsync();
        await DragAsync(100);

        // Act
        await Page.Locator("#handle").DblClickAsync();

        // Assert
        await Assert.That(await GetFirstSizeAsync()).IsEqualTo(300).Within(1);
        await Assert.That(await GetValueNowAsync()).IsEqualTo("50");
    }

    [Test]
    public async Task Load_WithStorageKey_RestoresSavedSize()
    {
        // Arrange
        await OpenAsync(attributes: """data-reu-split-key="test" """);
        await DragAsync(-150);

        // Act
        await OpenAsync(attributes: """data-reu-split-key="test" """);

        // Assert
        await Assert.That(await GetFirstSizeAsync()).IsEqualTo(150).Within(1);
    }

    [Test]
    public async Task Load_WithoutStorageKey_KeepsInitialSize()
    {
        // Arrange
        await OpenAsync();
        await DragAsync(-150);

        // Act
        await OpenAsync();

        // Assert
        await Assert.That(await GetFirstSizeAsync()).IsEqualTo(300).Within(1);
    }

    [Test]
    public async Task DoubleClick_WithStorageKey_ForgetsSavedSize()
    {
        // Arrange
        await OpenAsync(attributes: """data-reu-split-key="test" """);
        await DragAsync(-150);
        await Page.Locator("#handle").DblClickAsync();

        // Act
        await OpenAsync(attributes: """data-reu-split-key="test" """);

        // Assert
        await Assert.That(await GetFirstSizeAsync()).IsEqualTo(300).Within(1);
    }

    [Test]
    public async Task Render_LaterWithStorageKey_RestoresSavedSize()
    {
        // Arrange
        await OpenAsync(attributes: """data-reu-split-key="test" """);
        await DragAsync(-150);
        await GoToAsync("", Head);

        // Act
        await Page.EvaluateAsync($$"""
            html => document.body.insertAdjacentHTML("beforeend", html)
            """, Body(attributes: """data-reu-split-key="test" """));

        // Assert
        await Page.WaitForFunctionAsync("""document.getElementById("handle").hasAttribute("aria-valuenow")""");
        await Assert.That(await GetFirstSizeAsync()).IsEqualTo(150).Within(1);
    }

    [Test]
    public async Task Drag_InnerHandle_ResizesOnlyInnerPane()
    {
        // Arrange
        await GoToAsync("""
            <div id="outer" class="reu-split-pane" style="--reu-split-size: 50%; width: 600px; height: 300px">
                <div class="reu-split-first" id="first">
                    <div id="inner" class="reu-split-pane" style="--reu-split-size: 50%; height: 100%">
                        <div class="reu-split-first" id="inner-first">A</div>
                        <div class="reu-split-handle" id="handle" role="separator" tabindex="0"></div>
                        <div class="reu-split-second">B</div>
                    </div>
                </div>
                <div class="reu-split-handle" role="separator" tabindex="0"></div>
                <div class="reu-split-second">Second</div>
            </div>
            """, Head);
        await Page.WaitForFunctionAsync("""document.getElementById("handle").hasAttribute("aria-valuenow")""");

        // Act
        await DragAsync(-50);

        // Assert
        await Assert.That(await GetFirstSizeAsync()).IsEqualTo(300).Within(1);
        await Assert.That(await Page.EvaluateAsync<double>("""document.getElementById("inner-first").getBoundingClientRect().width""")).IsEqualTo(100).Within(1);
    }
}
