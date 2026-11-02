using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Reuniverse.Razor.Tests;

public class ReuLogsTests : BunitContext
{
    private static readonly DateTimeOffset Timestamp = new(2026, 1, 2, 3, 4, 5, 678, TimeSpan.Zero);

    private readonly BunitJSModuleInterop module;

    public ReuLogsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        module = JSInterop.SetupModule("./_content/Reuniverse.Razor/ReuLogs.razor.js");
        module.SetupVoid("scrollToBottom", _ => true).SetVoidResult();
    }

    private static ReuLogEntry CreateEntry(string message = "message", LogLevel level = LogLevel.Information, Exception? exception = null, IReadOnlyList<string>? scopes = null)
    {
        return new ReuLogEntry
        {
            Level = level,
            Timestamp = Timestamp,
            Category = "Test.Category",
            Message = message,
            Exception = exception,
            Scopes = scopes
        };
    }

    private static ReuLogEntry CreateStructuredEntry(string template, params KeyValuePair<string, object?>[] state)
    {
        return new ReuLogEntry
        {
            Timestamp = Timestamp,
            Message = template,
            MessageTemplate = template,
            State = state
        };
    }

    private static KeyValuePair<string, object?> Parameter(string name, object? value) => new(name, value);

    private IRenderedComponent<ReuLogs> RenderLogs(params ReuLogEntry[] entries)
    {
        return Render<ReuLogs>(parameters => parameters.Add(x => x.Entries, entries));
    }

    [Test]
    public async Task Render_WithEntries_ShowsMessages()
    {
        // Act
        var cut = RenderLogs(CreateEntry("first"), CreateEntry("second"));

        // Assert
        await cut.FindAll(".message").Select(x => x.TextContent.Trim()).ShouldEqualInOrderAsync("first", "second");
    }

    [Test]
    public async Task Render_WithoutEntries_ShowsNoRows()
    {
        // Act
        var cut = Render<ReuLogs>();

        // Assert
        await Assert.That(cut.FindAll(".entry").Count).IsEqualTo(0);
    }

    [Test]
    public async Task Render_ByDefault_ShowsInvariantTimestamp()
    {
        // Act
        var cut = RenderLogs(CreateEntry());

        // Assert
        await Assert.That(cut.Find(".timestamp").TextContent).IsEqualTo("03:04:05.678");
    }

    [Test]
    public async Task Render_WithShowTimestampOff_HidesTimestamp()
    {
        // Act
        var cut = Render<ReuLogs>(parameters => parameters
            .Add(x => x.Entries, [CreateEntry()])
            .Add(x => x.ShowTimestamp, false));

        // Assert
        await Assert.That(cut.FindAll(".timestamp").Count).IsEqualTo(0);
    }

    [Test]
    [Arguments(LogLevel.Trace, "TRC", "level-trace")]
    [Arguments(LogLevel.Debug, "DBG", "level-debug")]
    [Arguments(LogLevel.Information, "INF", "level-information")]
    [Arguments(LogLevel.Warning, "WRN", "level-warning")]
    [Arguments(LogLevel.Error, "ERR", "level-error")]
    [Arguments(LogLevel.Critical, "CRT", "level-critical")]
    [Arguments(LogLevel.None, "---", "level-none")]
    public async Task Render_WithLevel_ShowsLabelAndClass(LogLevel level, string label, string cssClass)
    {
        // Act
        var cut = RenderLogs(CreateEntry(level: level));

        // Assert
        await Assert.That(cut.Find(".level").TextContent).IsEqualTo(label);
        await Assert.That(cut.Find(".entry").ClassList.Contains(cssClass)).IsTrue();
    }

    [Test]
    public async Task Render_WithScopes_ShowsScopes()
    {
        // Act
        var cut = RenderLogs(CreateEntry(scopes: ["outer", "inner"]));

        // Assert
        await Assert.That(cut.Find(".scope").TextContent).IsEqualTo("outer → inner →");
    }

    [Test]
    public async Task Render_WithException_ShowsTypeAndMessage()
    {
        // Act
        var cut = RenderLogs(CreateEntry(exception: new InvalidOperationException("failed")));

        // Assert
        await Assert.That(cut.Find(".exception").TextContent).Contains("InvalidOperationException: failed");
    }

    [Test]
    public async Task Render_WithTemplate_ShowsParameterAsToken()
    {
        // Act
        var cut = RenderLogs(CreateStructuredEntry("User {UserId} joined", Parameter("UserId", 7)));

        // Assert
        await Assert.That(cut.Find(".param").TextContent).IsEqualTo("7");
        await Assert.That(cut.Find(".message").TextContent.Trim()).IsEqualTo("User 7 joined");
    }

    [Test]
    public async Task Render_WithUnknownToken_KeepsToken()
    {
        // Act
        var cut = RenderLogs(CreateStructuredEntry("Value {Missing}", Parameter("Other", 1)));

        // Assert
        await Assert.That(cut.Find(".message").TextContent.Trim()).IsEqualTo("Value {Missing}");
    }

    [Test]
    public async Task Render_WithEscapedBraces_UnescapesBraces()
    {
        // Act
        var cut = RenderLogs(CreateStructuredEntry("{{literal}} {Id}", Parameter("Id", 1)));

        // Assert
        await Assert.That(cut.Find(".message").TextContent.Trim()).IsEqualTo("{literal} 1");
    }

    [Test]
    public async Task Render_WithUnclosedBrace_KeepsBrace()
    {
        // Act
        var cut = RenderLogs(CreateStructuredEntry("Broken {Id", Parameter("Id", 1)));

        // Assert
        await Assert.That(cut.Find(".message").TextContent.Trim()).IsEqualTo("Broken {Id");
    }

    [Test]
    [Arguments("{@Id}")]
    [Arguments("{$Id}")]
    [Arguments("{Id,5}")]
    public async Task Render_WithTokenHint_ResolvesParameter(string token)
    {
        // Act
        var cut = RenderLogs(CreateStructuredEntry(token, Parameter("Id", 1)));

        // Assert
        await Assert.That(cut.Find(".param").TextContent).IsEqualTo("1");
    }

    [Test]
    public async Task Render_WithNullParameter_ShowsNull()
    {
        // Act
        var cut = RenderLogs(CreateStructuredEntry("{Id}", Parameter("Id", null)));

        // Assert
        await Assert.That(cut.Find(".param").TextContent).IsEqualTo("null");
    }

    [Test]
    public async Task Render_WithEnumerableParameter_JoinsValues()
    {
        // Act
        var cut = RenderLogs(CreateStructuredEntry("{Ids}", Parameter("Ids", new[] { 1, 2 })));

        // Assert
        await Assert.That(cut.Find(".param").TextContent).IsEqualTo("1, 2");
    }

    [Test]
    [Skip("Known bug: BuildSegments strips the format specifier and FormatValue ignores it, so this shows 1.2345")]
    public async Task Render_WithFormatSpecifier_AppliesFormat()
    {
        // Act
        var cut = RenderLogs(CreateStructuredEntry("{Value:F2}", Parameter("Value", 1.2345)));

        // Assert
        await Assert.That(cut.Find(".param").TextContent).IsEqualTo($"{1.2345:F2}");
    }

    [Test]
    [Skip("Known bug: TryGetValue finds parameters by name, so a repeated token shows the first value twice")]
    public async Task Render_WithRepeatedToken_ShowsEachValue()
    {
        // Act
        var cut = RenderLogs(CreateStructuredEntry("{A} {A}", Parameter("A", 1), Parameter("A", 2)));

        // Assert
        await Assert.That(cut.Find(".message").TextContent.Trim()).IsEqualTo("1 2");
    }

    [Test]
    public async Task ClickParameter_WithCallback_ReportsParameter()
    {
        // Arrange
        ReuLogParameter? clicked = null;
        var cut = Render<ReuLogs>(parameters => parameters
            .Add(x => x.Entries, [CreateStructuredEntry("{Id}", Parameter("Id", 7))])
            .Add(x => x.OnParameterClick, value => clicked = value));

        // Act
        cut.Find(".param").Click();

        // Assert
        await Assert.That(clicked).IsEqualTo(new ReuLogParameter("Id", 7));
    }

    [Test]
    public async Task ClickParameter_Once_SelectsParameter()
    {
        // Arrange
        var cut = RenderLogs(CreateStructuredEntry("{Id}", Parameter("Id", 7)));

        // Act
        cut.Find(".param").Click();

        // Assert
        await Assert.That(cut.Instance.SelectedParameter).IsEqualTo(new ReuLogParameter("Id", 7));
    }

    [Test]
    public async Task ClickParameter_Twice_ClearsSelection()
    {
        // Arrange
        var cut = RenderLogs(CreateStructuredEntry("{Id}", Parameter("Id", 7)));
        cut.Find(".param").Click();

        // Act
        cut.Find(".param").Click();

        // Assert
        await Assert.That(cut.Instance.SelectedParameter).IsNull();
    }

    [Test]
    public async Task ClickEntry_WithException_InvokesOnEntryClick()
    {
        // Arrange
        ReuLogEntry? clicked = null;
        var entry = CreateEntry(exception: new InvalidOperationException("failed"));
        var cut = Render<ReuLogs>(parameters => parameters
            .Add(x => x.Entries, [entry])
            .Add(x => x.OnEntryClick, value => clicked = value));

        // Act
        cut.Find(".entry").Click();

        // Assert
        await Assert.That(clicked).IsSameReferenceAs(entry);
    }

    [Test]
    public async Task ClickEntry_WithException_SelectsException()
    {
        // Arrange
        var exception = new InvalidOperationException("failed");
        var cut = RenderLogs(CreateEntry(exception: exception));

        // Act
        cut.Find(".entry").Click();

        // Assert
        await Assert.That(cut.Instance.SelectedException).IsSameReferenceAs(exception);
    }

    [Test]
    public async Task ClickEntry_WithoutException_DoesNotInvokeOnEntryClick()
    {
        // Arrange
        var clicked = false;
        var cut = Render<ReuLogs>(parameters => parameters
            .Add(x => x.Entries, [CreateEntry()])
            .Add(x => x.OnEntryClick, _ => clicked = true));

        // Act
        cut.Find(".entry").Click();

        // Assert
        await Assert.That(clicked).IsFalse();
    }

    [Test]
    public async Task Add_WithEntry_ShowsEntry()
    {
        // Arrange
        var cut = Render<ReuLogs>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.Add(CreateEntry("added")));

        // Assert
        await Assert.That(cut.Find(".message").TextContent.Trim()).IsEqualTo("added");
    }

    [Test]
    public async Task Add_WithNull_Throws()
    {
        // Arrange
        var cut = Render<ReuLogs>();

        // Act
        var act = () => cut.Instance.Add(null!);

        // Assert
        await Assert.That(act).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Add_BeyondMaxEntries_DropsOldestEntries()
    {
        // Arrange
        var cut = Render<ReuLogs>(parameters => parameters.Add(x => x.MaxEntries, 1));

        // Act
        await cut.InvokeAsync(() => cut.Instance.Add(CreateEntry("first")));
        await cut.InvokeAsync(() => cut.Instance.Add(CreateEntry("second")));

        // Assert
        await cut.Instance.GetEntries().Select(x => x.Message).ShouldEqualInOrderAsync("second");
    }

    [Test]
    public async Task Render_WithMaxEntriesBelowOne_KeepsOneEntry()
    {
        // Act
        var cut = Render<ReuLogs>(parameters => parameters
            .Add(x => x.Entries, [CreateEntry("first"), CreateEntry("second")])
            .Add(x => x.MaxEntries, 0));

        // Assert
        await cut.Instance.GetEntries().Select(x => x.Message).ShouldEqualInOrderAsync("second");
    }

    [Test]
    public async Task Clear_WithEntries_RemovesRows()
    {
        // Arrange
        var cut = RenderLogs(CreateEntry());

        // Act
        await cut.InvokeAsync(cut.Instance.Clear);

        // Assert
        await Assert.That(cut.FindAll(".entry").Count).IsEqualTo(0);
    }

    [Test]
    public async Task Render_WithNewEntriesInstance_ReloadsEntries()
    {
        // Arrange
        var cut = RenderLogs(CreateEntry("old"));

        // Act
        cut.Render(parameters => parameters.Add(x => x.Entries, [CreateEntry("new")]));

        // Assert
        await cut.Instance.GetEntries().Select(x => x.Message).ShouldEqualInOrderAsync("new");
    }

    [Test]
    public async Task Render_WithSameEntriesInstance_KeepsAddedEntries()
    {
        // Arrange
        ReuLogEntry[] entries = [CreateEntry("initial")];
        var cut = RenderLogs(entries);
        await cut.InvokeAsync(() => cut.Instance.Add(CreateEntry("added")));

        // Act
        cut.Render(parameters => parameters.Add(x => x.Entries, entries));

        // Assert
        await cut.Instance.GetEntries().Select(x => x.Message).ShouldEqualInOrderAsync("initial", "added");
    }

    [Test]
    public async Task Render_WithProvider_ShowsBufferedEntries()
    {
        // Arrange
        using var provider = new ReuLoggerProvider();
        provider.Add(CreateEntry("buffered"));

        // Act
        var cut = Render<ReuLogs>(parameters => parameters.Add(x => x.Provider, provider));

        // Assert
        await Assert.That(cut.Find(".message").TextContent.Trim()).IsEqualTo("buffered");
    }

    [Test]
    public async Task ProviderAdd_WithSubscribedComponent_ShowsEntry()
    {
        // Arrange
        using var provider = new ReuLoggerProvider();
        var cut = Render<ReuLogs>(parameters => parameters.Add(x => x.Provider, provider));

        // Act
        provider.Add(CreateEntry("live"));

        // Assert
        cut.WaitForAssertion(() => cut.Find(".entry"));
        await Assert.That(cut.Find(".message").TextContent.Trim()).IsEqualTo("live");
    }

    [Test]
    public async Task ProviderClear_WithSubscribedComponent_RemovesRows()
    {
        // Arrange
        using var provider = new ReuLoggerProvider();
        provider.Add(CreateEntry("entry"));
        var cut = Render<ReuLogs>(parameters => parameters.Add(x => x.Provider, provider));

        // Act
        provider.Clear();

        // Assert
        cut.WaitForState(() => cut.FindAll(".entry").Count == 0);
        await Assert.That(cut.Instance.GetEntries().Count).IsEqualTo(0);
    }

    [Test]
    public async Task Render_WithDifferentProvider_ReplacesEntries()
    {
        // Arrange
        using var first = new ReuLoggerProvider();
        using var second = new ReuLoggerProvider();
        first.Add(CreateEntry("first"));
        second.Add(CreateEntry("second"));
        var cut = Render<ReuLogs>(parameters => parameters.Add(x => x.Provider, first));

        // Act
        cut.Render(parameters => parameters.Add(x => x.Provider, second));

        // Assert
        await cut.Instance.GetEntries().Select(x => x.Message).ShouldEqualInOrderAsync("second");
    }

    [Test]
    public async Task Render_WithDifferentProvider_UnsubscribesFromPrevious()
    {
        // Arrange
        using var first = new ReuLoggerProvider();
        using var second = new ReuLoggerProvider();
        var cut = Render<ReuLogs>(parameters => parameters.Add(x => x.Provider, first));
        cut.Render(parameters => parameters.Add(x => x.Provider, second));

        // Act
        first.Add(CreateEntry("ignored"));

        // Assert
        await Assert.That(cut.Instance.GetEntries().Count).IsEqualTo(0);
    }

    [Test]
    public async Task DisposeAsync_WithProvider_Unsubscribes()
    {
        // Arrange
        using var provider = new ReuLoggerProvider();
        var cut = Render<ReuLogs>(parameters => parameters.Add(x => x.Provider, provider));

        // Act
        await cut.Instance.DisposeAsync();
        provider.Add(CreateEntry("ignored"));

        // Assert
        await Assert.That(cut.Instance.GetEntries().Count).IsEqualTo(0);
    }

    [Test]
    public async Task Render_WithEntries_ScrollsToBottom()
    {
        // Act
        RenderLogs(CreateEntry());

        // Assert
        await Assert.That(module.Invocations["scrollToBottom"].Count).IsEqualTo(1);
    }

    [Test]
    public async Task Render_WithScrollToBottomOff_DoesNotScroll()
    {
        // Act
        Render<ReuLogs>(parameters => parameters
            .Add(x => x.Entries, [CreateEntry()])
            .Add(x => x.ScrollToBottom, false));

        // Assert
        await Assert.That(module.Invocations["scrollToBottom"].Count).IsEqualTo(0);
    }

    [Test]
    public async Task ExportAsText_WithEntry_FormatsLine()
    {
        // Arrange
        var cut = RenderLogs(CreateEntry("message", LogLevel.Error, new InvalidOperationException("failed"), ["outer", "inner"]));

        // Act
        var text = cut.Instance.ExportAsText();

        // Assert
        await Assert.That(text).IsEqualTo($"[03:04:05.678] ERR Test.Category outer → inner message  InvalidOperationException: failed{Environment.NewLine}");
    }

    [Test]
    public async Task ExportAsText_WithoutCategoryOrScopes_LeavesThemOut()
    {
        // Arrange
        var cut = RenderLogs(new ReuLogEntry { Timestamp = Timestamp, Message = "message" });

        // Act
        var text = cut.Instance.ExportAsText();

        // Assert
        await Assert.That(text).IsEqualTo($"[03:04:05.678] TRC message{Environment.NewLine}");
    }

    [Test]
    public async Task ExportAsCsv_WithEntry_WritesHeaderAndRow()
    {
        // Arrange
        var cut = RenderLogs(CreateEntry());

        // Act
        var csv = cut.Instance.ExportAsCsv();

        // Assert
        var expectedRow = "2026-01-02 03:04:05.678 +00:00,Information,Test.Category,,message,";
        await Assert.That(csv).IsEqualTo($"Timestamp,Level,Category,Scopes,Message,Exception{Environment.NewLine}{expectedRow}{Environment.NewLine}");
    }

    [Test]
    [Arguments("a,b", "\"a,b\"")]
    [Arguments("say \"hi\"", "\"say \"\"hi\"\"\"")]
    [Arguments("two\nlines", "\"two\nlines\"")]
    [Arguments("plain", "plain")]
    public async Task ExportAsCsv_WithSpecialCharacters_EscapesMessage(string message, string expected)
    {
        // Arrange
        var cut = RenderLogs(CreateEntry(message));

        // Act
        var csv = cut.Instance.ExportAsCsv();

        // Assert
        await Assert.That(csv).Contains($",,{expected},");
    }

    [Test]
    public async Task ExportAsJson_WithEntry_WritesProperties()
    {
        // Arrange
        var cut = RenderLogs(CreateEntry("message", LogLevel.Warning, scopes: ["outer"]));

        // Act
        using var json = JsonDocument.Parse(cut.Instance.ExportAsJson());

        // Assert
        var entry = json.RootElement[0];
        await Assert.That(entry.GetProperty("timestamp").GetString()).IsEqualTo("2026-01-02 03:04:05.678 +00:00");
        await Assert.That(entry.GetProperty("level").GetString()).IsEqualTo("Warning");
        await Assert.That(entry.GetProperty("category").GetString()).IsEqualTo("Test.Category");
        await Assert.That(entry.GetProperty("scopes")[0].GetString()).IsEqualTo("outer");
        await Assert.That(entry.GetProperty("message").GetString()).IsEqualTo("message");
        await Assert.That(entry.GetProperty("exception").ValueKind).IsEqualTo(JsonValueKind.Null);
    }

    [Test]
    public async Task ExportAsJson_WithNonAsciiScope_KeepsTextReadable()
    {
        // Arrange
        var cut = RenderLogs(CreateEntry(scopes: ["→"]));

        // Act
        var json = cut.Instance.ExportAsJson();

        // Assert
        await Assert.That(json).Contains("→");
    }
}
