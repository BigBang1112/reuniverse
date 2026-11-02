using Microsoft.Extensions.Logging;

namespace Reuniverse.Razor.Tests;

public class ReuLoggerProviderTests
{
    private const string Category = "Test.Category";

    private static ReuLogEntry CreateEntry(string message) => new() { Message = message };

    [Test]
    public async Task Add_BeyondCapacity_DropsOldestEntries()
    {
        // Arrange
        using var provider = new ReuLoggerProvider(capacity: 2);

        // Act
        provider.Add(CreateEntry("first"));
        provider.Add(CreateEntry("second"));
        provider.Add(CreateEntry("third"));

        // Assert
        await provider.GetSnapshot().Select(x => x.Message).ShouldEqualInOrderAsync("second", "third");
    }

    [Test]
    [Arguments(0)]
    [Arguments(-5)]
    public async Task Constructor_WithCapacityBelowOne_UsesOne(int capacity)
    {
        // Act
        using var provider = new ReuLoggerProvider(capacity);

        // Assert
        await Assert.That(provider.Capacity).IsEqualTo(1);
    }

    [Test]
    public async Task Add_WithNull_Throws()
    {
        // Arrange
        using var provider = new ReuLoggerProvider();

        // Act
        var act = () => provider.Add(null!);

        // Assert
        await Assert.That(act).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Add_WithSubscriber_RaisesEntryAdded()
    {
        // Arrange
        using var provider = new ReuLoggerProvider();
        ReuLogEntry? raised = null;
        provider.EntryAdded += entry => raised = entry;
        var added = CreateEntry("added");

        // Act
        provider.Add(added);

        // Assert
        await Assert.That(raised).IsSameReferenceAs(added);
    }

    [Test]
    public async Task Clear_WithEntries_RemovesEntries()
    {
        // Arrange
        using var provider = new ReuLoggerProvider();
        provider.Add(CreateEntry("entry"));

        // Act
        provider.Clear();

        // Assert
        await Assert.That(provider.GetSnapshot().Count).IsEqualTo(0);
    }

    [Test]
    public async Task Clear_WithSubscriber_RaisesCleared()
    {
        // Arrange
        using var provider = new ReuLoggerProvider();
        var raised = false;
        provider.Cleared += () => raised = true;

        // Act
        provider.Clear();

        // Assert
        await Assert.That(raised).IsTrue();
    }

    [Test]
    public async Task Dispose_WithSubscriber_RemovesSubscriber()
    {
        // Arrange
        var provider = new ReuLoggerProvider();
        var raised = false;
        provider.EntryAdded += _ => raised = true;

        // Act
        provider.Dispose();
        provider.Add(CreateEntry("entry"));

        // Assert
        await Assert.That(raised).IsFalse();
    }

    [Test]
    [Arguments(LogLevel.Trace, LogLevel.Trace, true)]
    [Arguments(LogLevel.Warning, LogLevel.Information, false)]
    [Arguments(LogLevel.Warning, LogLevel.Warning, true)]
    [Arguments(LogLevel.Warning, LogLevel.Error, true)]
    [Arguments(LogLevel.Trace, LogLevel.None, false)]
    public async Task IsEnabled_WithMinimumLevel_ComparesLevels(LogLevel minimumLevel, LogLevel level, bool expected)
    {
        // Arrange
        using var provider = new ReuLoggerProvider { MinimumLevel = minimumLevel };
        var logger = provider.CreateLogger(Category);

        // Act
        var enabled = logger.IsEnabled(level);

        // Assert
        await Assert.That(enabled).IsEqualTo(expected);
    }

    [Test]
    public async Task Log_BelowMinimumLevel_CapturesNothing()
    {
        // Arrange
        using var provider = new ReuLoggerProvider { MinimumLevel = LogLevel.Warning };
        var logger = provider.CreateLogger(Category);

        // Act
        logger.LogInformation("ignored");

        // Assert
        await Assert.That(provider.GetSnapshot().Count).IsEqualTo(0);
    }

    [Test]
    public async Task Log_WithTemplate_CapturesEntry()
    {
        // Arrange
        using var provider = new ReuLoggerProvider();
        var logger = provider.CreateLogger(Category);
        var exception = new InvalidOperationException("failed");

        // Act
        logger.LogError(new EventId(42), exception, "User {UserId} failed", 7);

        // Assert
        var entry = provider.GetSnapshot().Single();
        await Assert.That(entry.Level).IsEqualTo(LogLevel.Error);
        await Assert.That(entry.Category).IsEqualTo(Category);
        await Assert.That(entry.EventId.Id).IsEqualTo(42);
        await Assert.That(entry.Message).IsEqualTo("User 7 failed");
        await Assert.That(entry.MessageTemplate).IsEqualTo("User {UserId} failed");
        await Assert.That(entry.Exception).IsSameReferenceAs(exception);
    }

    [Test]
    public async Task Log_WithTemplate_CapturesStructuredValues()
    {
        // Arrange
        using var provider = new ReuLoggerProvider();
        var logger = provider.CreateLogger(Category);

        // Act
        logger.LogInformation("User {UserId} joined", 7);

        // Assert
        var state = provider.GetSnapshot().Single().State!;
        await Assert.That(state.Single(x => x.Key == "UserId").Value).IsEqualTo(7);
    }

    [Test]
    public async Task Log_WithPlainState_HasNoTemplate()
    {
        // Arrange
        using var provider = new ReuLoggerProvider();
        var logger = provider.CreateLogger(Category);

        // Act
        logger.Log(LogLevel.Information, default, "plain", null, (state, _) => state);

        // Assert
        var entry = provider.GetSnapshot().Single();
        await Assert.That(entry.Message).IsEqualTo("plain");
        await Assert.That(entry.MessageTemplate).IsNull();
    }

    [Test]
    public async Task Log_WithoutFormatter_Throws()
    {
        // Arrange
        using var provider = new ReuLoggerProvider();
        var logger = provider.CreateLogger(Category);

        // Act
        var act = () => logger.Log(LogLevel.Information, default, "plain", null, null!);

        // Assert
        await Assert.That(act).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Log_WithoutScope_HasNoScopes()
    {
        // Arrange
        using var provider = new ReuLoggerProvider();
        var logger = provider.CreateLogger(Category);

        // Act
        logger.LogInformation("message");

        // Assert
        await Assert.That(provider.GetSnapshot().Single().Scopes).IsNull();
    }

    [Test]
    public async Task Log_InsideNestedScopes_CapturesScopesOutermostFirst()
    {
        // Arrange
        using var provider = new ReuLoggerProvider();
        var logger = provider.CreateLogger(Category);

        // Act
        using (logger.BeginScope("outer"))
        using (logger.BeginScope("inner"))
        {
            logger.LogInformation("message");
        }

        // Assert
        await provider.GetSnapshot().Single().Scopes!.ShouldEqualInOrderAsync("outer", "inner");
    }

    [Test]
    public async Task Log_AfterScopeDisposed_LeavesScopeOut()
    {
        // Arrange
        using var provider = new ReuLoggerProvider();
        var logger = provider.CreateLogger(Category);

        using (logger.BeginScope("outer"))
        {
            using (logger.BeginScope("inner"))
            {
            }

            // Act
            logger.LogInformation("message");
        }

        // Assert
        await provider.GetSnapshot().Single().Scopes!.ShouldEqualInOrderAsync("outer");
    }
}
