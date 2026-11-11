using AngleSharp.Dom;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Reuniverse.Razor.Tests;

public class ReuHexEditorTests : BunitContext
{
    private readonly BunitJSModuleInterop module;

    public ReuHexEditorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        module = JSInterop.SetupModule("./_content/Reuniverse.Razor/ReuHexEditor.razor.js");
        module.SetupVoid("init", _ => true).SetVoidResult();
        module.SetupVoid("scrollToRow", _ => true).SetVoidResult();
        module.SetupVoid("copyText", _ => true).SetVoidResult();
        module.SetupVoid("dispose", _ => true).SetVoidResult();
    }

    private IRenderedComponent<ReuHexEditor> RenderEditor(byte[]? data, Action<ComponentParameterCollectionBuilder<ReuHexEditor>>? configure = null)
    {
        return Render<ReuHexEditor>(parameters =>
        {
            parameters.Add(x => x.Data, data);
            configure?.Invoke(parameters);
        });
    }

    private static IReadOnlyList<IElement> Rows(IRenderedComponent<ReuHexEditor> cut) => cut.FindAll(".hex-row:not(.hex-header)");

    private static IElement HexCell(IRenderedComponent<ReuHexEditor> cut, int offset) => cut.FindAll(".hex-row:not(.hex-header) .hex .b")[offset];

    private static IElement TextCell(IRenderedComponent<ReuHexEditor> cut, int offset) => cut.FindAll(".hex-row:not(.hex-header) .text .b")[offset];

    private static void Press(IRenderedComponent<ReuHexEditor> cut, string key, bool shift = false, bool ctrl = false)
    {
        cut.Find(".hex-scroll").KeyDown(new KeyboardEventArgs { Key = key, ShiftKey = shift, CtrlKey = ctrl });
    }

    private static void Type(IRenderedComponent<ReuHexEditor> cut, string text)
    {
        foreach (var key in text)
        {
            Press(cut, key.ToString());
        }
    }

    [Test]
    public async Task Render_WithData_ShowsOffsetsHexAndText()
    {
        // Act
        var cut = RenderEditor([.. "Hello, world!\0\x01\x7F\xFF"u8, 0x41, 0x42, 0x43]);

        // Assert
        var rows = Rows(cut);
        await Assert.That(rows.Count).IsEqualTo(2);
        await Assert.That(rows[0].QuerySelector(".offset")!.TextContent).IsEqualTo("00000000");
        await Assert.That(rows[1].QuerySelector(".offset")!.TextContent).IsEqualTo("00000010");
        await Assert.That(HexCell(cut, 1).TextContent).IsEqualTo("65");
        await Assert.That(TextCell(cut, 0).TextContent).IsEqualTo("H");
        await Assert.That(TextCell(cut, 13).TextContent).IsEqualTo(".");
        await Assert.That(TextCell(cut, 15).TextContent).IsEqualTo(".");
    }

    [Test]
    public async Task Render_WithBytesPerRow_SplitsRowsByIt()
    {
        // Act
        var cut = RenderEditor(new byte[20], parameters => parameters.Add(x => x.BytesPerRow, 8));

        // Assert
        var rows = Rows(cut);
        await Assert.That(rows.Count).IsEqualTo(3);
        await Assert.That(rows[2].QuerySelectorAll(".hex .b").Length).IsEqualTo(4);
        await Assert.That(cut.FindAll(".hex-header .hex .b").Count).IsEqualTo(8);
    }

    [Test]
    [Arguments(null)]
    [Arguments(new byte[0])]
    public async Task Render_WithoutData_ShowsEmpty(byte[]? data)
    {
        // Act
        var cut = RenderEditor(data);

        // Assert
        await Assert.That(cut.FindAll(".empty").Count).IsEqualTo(1);
        await Assert.That(cut.FindAll(".hex-status").Count).IsEqualTo(0);
    }

    [Test]
    public async Task Render_Zero_IsDimmed()
    {
        // Act
        var cut = RenderEditor([0, 1]);

        // Assert
        await Assert.That(HexCell(cut, 0).ClassList.Contains("zero")).IsTrue();
        await Assert.That(HexCell(cut, 1).ClassList.Contains("zero")).IsFalse();
    }

    [Test]
    public async Task Render_WithHighlight_ColorsRangeWithTooltip()
    {
        // Act
        var cut = RenderEditor(new byte[8], parameters => parameters
            .Add(x => x.Highlights, [new ReuHexHighlight(2, 3, "red", "Chunk ID")]));

        // Assert
        await Assert.That(HexCell(cut, 1).ClassList.Contains("highlight")).IsFalse();
        await Assert.That(HexCell(cut, 2).ClassList.Contains("highlight")).IsTrue();
        await Assert.That(HexCell(cut, 4).GetAttribute("style")).IsEqualTo("--reu-hex-highlight: red;");
        await Assert.That(TextCell(cut, 4).GetAttribute("data-reu-tooltip")).IsEqualTo("Chunk ID");
        await Assert.That(HexCell(cut, 5).ClassList.Contains("highlight")).IsFalse();
    }

    [Test]
    public async Task Render_Default_PutsCursorOnFirstByte()
    {
        // Act
        var cut = RenderEditor([0x41, 0x42]);

        // Assert
        await Assert.That(HexCell(cut, 0).ClassList.Contains("cursor")).IsTrue();
        await Assert.That(TextCell(cut, 0).ClassList.Contains("cursor-other")).IsTrue();
        await Assert.That(cut.Instance.Selection).IsEqualTo(new ReuHexSelection(0, 1));
    }

    [Test]
    public async Task MouseDown_OnByte_MovesCursorAndShowsItInStatus()
    {
        // Arrange
        var cut = RenderEditor([0x00, 0x4A]);

        // Act
        HexCell(cut, 1).MouseDown(new MouseEventArgs { Button = 0 });

        // Assert
        await Assert.That(HexCell(cut, 1).ClassList.Contains("cursor")).IsTrue();
        var status = cut.Find(".hex-status").TextContent;
        await Assert.That(status).Contains("0x00000001");
        await Assert.That(status).Contains("0x4A");
        await Assert.That(status).Contains("'J'");
    }

    [Test]
    public async Task MouseDown_WithShift_SelectsRange()
    {
        // Arrange
        var cut = RenderEditor(new byte[8]);
        HexCell(cut, 2).MouseDown(new MouseEventArgs { Button = 0 });

        // Act
        HexCell(cut, 5).MouseDown(new MouseEventArgs { Button = 0, ShiftKey = true });

        // Assert
        await Assert.That(cut.Instance.Selection).IsEqualTo(new ReuHexSelection(2, 4));
        await Assert.That(HexCell(cut, 3).ClassList.Contains("selected")).IsTrue();
        await Assert.That(TextCell(cut, 3).ClassList.Contains("selected")).IsTrue();
        await Assert.That(cut.Find(".hex-status").TextContent).Contains("Selected 4 bytes");
    }

    [Test]
    public async Task MouseOver_WithoutPressedButton_IsNotListenedTo()
    {
        // Act
        var cut = RenderEditor(new byte[8]);

        // Assert
        await Assert.That(() => HexCell(cut, 3).MouseOver()).Throws<MissingEventHandlerException>();
    }

    [Test]
    public async Task MouseOver_WhileDragging_ExtendsSelection()
    {
        // Arrange
        var cut = RenderEditor(new byte[8]);
        HexCell(cut, 1).MouseDown(new MouseEventArgs { Button = 0 });

        // Act
        HexCell(cut, 4).MouseOver(new MouseEventArgs { Buttons = 1 });

        // Assert
        await Assert.That(cut.Instance.Selection).IsEqualTo(new ReuHexSelection(1, 4));
    }

    [Test]
    public async Task MouseUp_AfterDragging_StopsListeningToMouseOver()
    {
        // Arrange
        var cut = RenderEditor(new byte[8]);
        HexCell(cut, 1).MouseDown(new MouseEventArgs { Button = 0 });

        // Act
        cut.Find(".hex-scroll").MouseUp();

        // Assert
        await Assert.That(() => HexCell(cut, 3).MouseOver(new MouseEventArgs { Buttons = 1 })).Throws<MissingEventHandlerException>();
    }

    [Test]
    [Arguments("ArrowRight", false, 1)]
    [Arguments("ArrowLeft", false, 0)]
    [Arguments("ArrowDown", false, 16)]
    [Arguments("PageDown", false, 39)]
    [Arguments("End", false, 15)]
    [Arguments("End", true, 39)]
    public async Task KeyDown_Navigation_MovesCursor(string key, bool ctrl, int expected)
    {
        // Arrange
        var cut = RenderEditor(new byte[40]);

        // Act
        Press(cut, key, ctrl: ctrl);

        // Assert
        await Assert.That(cut.Instance.Selection).IsEqualTo(new ReuHexSelection(expected, 1));
    }

    [Test]
    public async Task KeyDown_HomeOnLaterRow_GoesToRowStart()
    {
        // Arrange
        var cut = RenderEditor(new byte[40]);
        HexCell(cut, 21).MouseDown(new MouseEventArgs { Button = 0 });

        // Act
        Press(cut, "Home");

        // Assert
        await Assert.That(cut.Instance.Selection.Start).IsEqualTo(16);
    }

    [Test]
    public async Task KeyDown_WithShift_ExtendsSelectionAndNotifies()
    {
        // Arrange
        var selections = new List<ReuHexSelection>();
        var cut = RenderEditor(new byte[8], parameters => parameters
            .Add(x => x.SelectionChanged, EventCallback.Factory.Create<ReuHexSelection>(this, selections.Add)));

        // Act
        Press(cut, "ArrowRight", shift: true);
        Press(cut, "ArrowRight", shift: true);

        // Assert
        await selections.ShouldEqualInOrderAsync(new ReuHexSelection(0, 2), new ReuHexSelection(0, 3));
    }

    [Test]
    public async Task KeyDown_Navigation_ScrollsCursorRowIntoView()
    {
        // Arrange
        var cut = RenderEditor(new byte[40]);

        // Act
        Press(cut, "ArrowDown");

        // Assert
        var call = module.Invocations["scrollToRow"].Last();
        await Assert.That(call.Arguments[1]).IsEqualTo(1);
    }

    [Test]
    public async Task KeyDown_CtrlA_SelectsAll()
    {
        // Arrange
        var cut = RenderEditor(new byte[40]);

        // Act
        Press(cut, "a", ctrl: true);

        // Assert
        await Assert.That(cut.Instance.Selection).IsEqualTo(new ReuHexSelection(0, 40));
    }

    [Test]
    public async Task Type_HexDigits_EditsByteAndMovesOn()
    {
        // Arrange
        var data = new byte[4];
        var edits = new List<ReuHexEdit>();
        var cut = RenderEditor(data, parameters => parameters
            .Add(x => x.OnEdit, EventCallback.Factory.Create<ReuHexEdit>(this, edits.Add)));

        // Act
        Type(cut, "4a");

        // Assert
        await Assert.That(data[0]).IsEqualTo((byte)0x4A);
        await Assert.That(cut.Instance.Selection.Start).IsEqualTo(1);
        await edits.ShouldEqualInOrderAsync(new ReuHexEdit(0, 0x00, 0x40), new ReuHexEdit(0, 0x40, 0x4A));
    }

    [Test]
    public async Task Type_FirstDigit_MarksSecondDigitAsNext()
    {
        // Arrange
        var cut = RenderEditor(new byte[4]);

        // Act
        Type(cut, "7");

        // Assert
        var nibbles = HexCell(cut, 0).QuerySelectorAll(".nibble");
        await Assert.That(nibbles[0].TextContent).IsEqualTo("7");
        await Assert.That(nibbles[1].ClassList.Contains("active")).IsTrue();
    }

    [Test]
    public async Task Type_OnLastByte_StaysOnIt()
    {
        // Arrange
        var data = new byte[2];
        var cut = RenderEditor(data);
        HexCell(cut, 1).MouseDown(new MouseEventArgs { Button = 0 });

        // Act
        Type(cut, "ffee");

        // Assert
        await Assert.That(data[1]).IsEqualTo((byte)0xEE);
        await Assert.That(cut.Instance.Selection.Start).IsEqualTo(1);
    }

    [Test]
    public async Task Type_InTextColumn_WritesCharacter()
    {
        // Arrange
        var data = new byte[4];
        var cut = RenderEditor(data);
        TextCell(cut, 1).MouseDown(new MouseEventArgs { Button = 0 });

        // Act
        Type(cut, "Hi");

        // Assert
        await Assert.That(data).IsEquivalentTo(new byte[] { 0, (byte)'H', (byte)'i', 0 });
        await Assert.That(cut.Instance.Selection.Start).IsEqualTo(3);
    }

    [Test]
    public async Task Type_NonHexInHexColumn_IsIgnored()
    {
        // Arrange
        var data = new byte[4];
        var cut = RenderEditor(data);

        // Act
        Type(cut, "g ");

        // Assert
        await Assert.That(data).IsEquivalentTo(new byte[4]);
    }

    [Test]
    public async Task Type_WhenReadOnly_IsIgnored()
    {
        // Arrange
        var data = new byte[4];
        var cut = RenderEditor(data, parameters => parameters.Add(x => x.ReadOnly, true));

        // Act
        Type(cut, "ff");

        // Assert
        await Assert.That(data).IsEquivalentTo(new byte[4]);
        await Assert.That(cut.Find(".hex-status").TextContent).Contains("Read-only");
    }

    [Test]
    public async Task Type_ChangedByte_IsMarkedAsModified()
    {
        // Arrange
        var cut = RenderEditor(new byte[4]);

        // Act
        Type(cut, "12");

        // Assert
        await Assert.That(HexCell(cut, 0).ClassList.Contains("modified")).IsTrue();
        await Assert.That(HexCell(cut, 1).ClassList.Contains("modified")).IsFalse();
    }

    [Test]
    public async Task Type_SameValueAgain_IsNotMarkedAsModified()
    {
        // Arrange
        var cut = RenderEditor([0x12, 0]);

        // Act
        Type(cut, "12");

        // Assert
        await Assert.That(HexCell(cut, 0).ClassList.Contains("modified")).IsFalse();
    }

    [Test]
    public async Task Undo_AfterTypingByte_RevertsItInOneStep()
    {
        // Arrange
        var data = new byte[] { 0x11, 0x22 };
        var history = new ReuObjectTreeHistory();
        var cut = RenderEditor(data, parameters => parameters.Add(x => x.History, history));
        Type(cut, "ab");

        // Act
        await cut.InvokeAsync(() => history.Undo());

        // Assert
        await Assert.That(data[0]).IsEqualTo((byte)0x11);
        await Assert.That(history.CanUndo).IsFalse();
        await Assert.That(HexCell(cut, 0).TextContent).IsEqualTo("11");
        await Assert.That(HexCell(cut, 0).ClassList.Contains("modified")).IsFalse();
    }

    [Test]
    public async Task KeyDown_CtrlZAndCtrlY_UndoAndRedo()
    {
        // Arrange
        var data = new byte[] { 0x11, 0x22 };
        var history = new ReuObjectTreeHistory();
        var cut = RenderEditor(data, parameters => parameters.Add(x => x.History, history));
        Type(cut, "ab");

        // Act
        Press(cut, "z", ctrl: true);
        var undone = data[0];
        Press(cut, "y", ctrl: true);

        // Assert
        await Assert.That(undone).IsEqualTo((byte)0x11);
        await Assert.That(data[0]).IsEqualTo((byte)0xAB);
    }

    [Test]
    public async Task KeyDown_CtrlShiftZ_Redoes()
    {
        // Arrange
        var data = new byte[] { 0x11, 0x22 };
        var history = new ReuObjectTreeHistory();
        var cut = RenderEditor(data, parameters => parameters.Add(x => x.History, history));
        Type(cut, "ab");
        Press(cut, "z", ctrl: true);

        // Act
        Press(cut, "Z", shift: true, ctrl: true);

        // Assert
        await Assert.That(data[0]).IsEqualTo((byte)0xAB);
    }

    [Test]
    public async Task KeyDown_CtrlC_CopiesSelectionAsHex()
    {
        // Arrange
        var cut = RenderEditor([0x41, 0x42, 0x00, 0x43]);
        Press(cut, "ArrowRight", shift: true);
        Press(cut, "ArrowRight", shift: true);

        // Act
        Press(cut, "c", ctrl: true);

        // Assert
        await Assert.That(module.Invocations["copyText"].Single().Arguments[0]).IsEqualTo("41 42 00");
    }

    [Test]
    public async Task KeyDown_CtrlCInTextColumn_CopiesSelectionAsText()
    {
        // Arrange
        var cut = RenderEditor([0x41, 0x42, 0x00, 0x43]);
        TextCell(cut, 0).MouseDown(new MouseEventArgs { Button = 0 });
        TextCell(cut, 3).MouseDown(new MouseEventArgs { Button = 0, ShiftKey = true });

        // Act
        Press(cut, "c", ctrl: true);

        // Assert
        await Assert.That(module.Invocations["copyText"].Single().Arguments[0]).IsEqualTo("AB.C");
    }

    [Test]
    public async Task SelectAsync_WithRange_SelectsAndScrollsToIt()
    {
        // Arrange
        var cut = RenderEditor(new byte[100]);

        // Act
        await cut.InvokeAsync(() => cut.Instance.SelectAsync(50, 4));

        // Assert
        await Assert.That(cut.Instance.Selection).IsEqualTo(new ReuHexSelection(50, 4));
        await Assert.That(module.Invocations["scrollToRow"].Last().Arguments[1]).IsEqualTo(3);
    }

    [Test]
    public async Task Render_WithNewData_ResetsCursorAndModifiedMarks()
    {
        // Arrange
        var cut = RenderEditor(new byte[40]);
        Press(cut, "ArrowDown");
        Type(cut, "ff");

        // Act
        cut.Render(parameters => parameters.Add(x => x.Data, new byte[40]));

        // Assert
        await Assert.That(cut.Instance.Selection).IsEqualTo(new ReuHexSelection(0, 1));
        await Assert.That(cut.FindAll(".modified").Count).IsEqualTo(0);
    }

    [Test]
    public async Task Render_WithStyle_KeepsItAlongsideVariables()
    {
        // Act
        var cut = RenderEditor(new byte[4], parameters => parameters.AddUnmatched("style", "height: 200px"));

        // Assert
        await Assert.That(cut.Find(".reu-hex-editor").GetAttribute("style")).IsEqualTo("--reu-hex-columns: 16; --reu-hex-row: 20px; height: 200px");
    }

    [Test]
    public async Task DisposeAsync_WhenRendered_RemovesKeyHandler()
    {
        // Arrange
        RenderEditor(new byte[4]);

        // Act
        await DisposeComponentsAsync();

        // Assert
        await Assert.That(module.Invocations["dispose"].Count).IsEqualTo(1);
    }
}
