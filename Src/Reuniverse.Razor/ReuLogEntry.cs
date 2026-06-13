using Microsoft.Extensions.Logging;

namespace Reuniverse.Razor;

/// <summary>
/// Represents a single log entry displayed by <see cref="ReuLogs"/>.
/// </summary>
public sealed class ReuLogEntry
{
    /// <summary>
    /// The severity level of the log entry.
    /// </summary>
    public LogLevel Level { get; init; }

    /// <summary>
    /// The moment the entry was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    /// <summary>
    /// The logger category name the entry originated from, if any.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// The event id associated with the entry.
    /// </summary>
    public EventId EventId { get; init; }

    /// <summary>
    /// The fully formatted log message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// The original message template (e.g. <c>User {UserId} logged in</c>), if available.
    /// </summary>
    public string? MessageTemplate { get; init; }

    /// <summary>
    /// The structured state values used to render parameterized messages, if available.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, object?>>? State { get; init; }

    /// <summary>
    /// The exception attached to the entry, if any.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// The ordered list of active scope strings at the time the entry was created, outermost first.
    /// </summary>
    public IReadOnlyList<string>? Scopes { get; init; }

    /// <summary>
    /// Whether the entry carries an exception.
    /// </summary>
    public bool HasException => Exception is not null;
}

/// <summary>
/// Represents a single named parameter rendered within a log message.
/// </summary>
/// <param name="Name">The parameter name from the message template.</param>
/// <param name="Value">The parameter value.</param>
public readonly record struct ReuLogParameter(string Name, object? Value);
