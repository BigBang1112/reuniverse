using Microsoft.Extensions.Logging;

namespace Reuniverse.Razor;

/// <summary>
/// An <see cref="ILoggerProvider"/> that captures log entries into an in-memory buffer so they can be
/// displayed live by the <see cref="ReuLogs"/> component.
/// </summary>
/// <remarks>
/// Register it on the logging pipeline, for example:
/// <code>
/// var provider = new ReuLoggerProvider();
/// builder.Logging.AddProvider(provider);
/// // ...then pass the same instance to &lt;ReuLogs Provider="provider" /&gt;
/// </code>
/// </remarks>
public sealed class ReuLoggerProvider : ILoggerProvider
{
    private readonly Queue<ReuLogEntry> entries = new();
    private readonly object gate = new();

    /// <summary>
    /// Raised whenever a new entry is appended to the buffer.
    /// </summary>
    public event Action<ReuLogEntry>? EntryAdded;

    /// <summary>
    /// Raised whenever the buffer is cleared.
    /// </summary>
    public event Action? Cleared;

    /// <summary>
    /// The maximum number of entries kept in the buffer. Older entries are discarded once the limit is reached.
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    /// The minimum level an entry must have to be captured.
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReuLoggerProvider"/> class.
    /// </summary>
    /// <param name="capacity">The maximum number of entries to keep in the buffer.</param>
    public ReuLoggerProvider(int capacity = 1000)
    {
        Capacity = capacity < 1 ? 1 : capacity;
    }

    /// <summary>
    /// Gets a snapshot of the currently buffered entries, oldest first.
    /// </summary>
    public IReadOnlyList<ReuLogEntry> GetSnapshot()
    {
        lock (gate)
        {
            return entries.ToArray();
        }
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new ReuLogger(this, categoryName);

    /// <summary>
    /// Adds an entry to the buffer and notifies listeners.
    /// </summary>
    public void Add(ReuLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        lock (gate)
        {
            entries.Enqueue(entry);

            while (entries.Count > Capacity)
            {
                entries.Dequeue();
            }
        }

        EntryAdded?.Invoke(entry);
    }

    /// <summary>
    /// Removes all buffered entries and notifies listeners.
    /// </summary>
    public void Clear()
    {
        lock (gate)
        {
            entries.Clear();
        }

        Cleared?.Invoke();
    }

    internal bool IsEnabled(LogLevel level) => level != LogLevel.None && level >= MinimumLevel;

    /// <inheritdoc />
    public void Dispose()
    {
        EntryAdded = null;
        Cleared = null;
    }

    private sealed class ReuLogger(ReuLoggerProvider provider, string categoryName) : ILogger
    {
        private static readonly AsyncLocal<ScopeNode?> ScopeStack = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            var text = state.ToString() ?? string.Empty;
            var previous = ScopeStack.Value;
            ScopeStack.Value = new ScopeNode(text, previous);
            return new ScopeDisposable(previous);
        }

        private static IReadOnlyList<string>? CaptureScopes()
        {
            var node = ScopeStack.Value;
            if (node is null) return null;
            var list = new List<string>();
            while (node is not null)
            {
                list.Add(node.Value);
                node = node.Parent;
            }
            list.Reverse();
            return list;
        }

        public bool IsEnabled(LogLevel logLevel) => provider.IsEnabled(logLevel);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(formatter);

            var stateValues = state as IReadOnlyList<KeyValuePair<string, object?>>;

            string? template = null;

            if (stateValues is not null)
            {
                for (var i = 0; i < stateValues.Count; i++)
                {
                    if (stateValues[i].Key == "{OriginalFormat}")
                    {
                        template = stateValues[i].Value?.ToString();
                        break;
                    }
                }
            }

            provider.Add(new ReuLogEntry
            {
                Level = logLevel,
                Timestamp = DateTimeOffset.Now,
                Category = categoryName,
                EventId = eventId,
                Message = formatter(state, exception),
                MessageTemplate = template,
                State = stateValues,
                Exception = exception,
                Scopes = CaptureScopes()
            });
        }

        private sealed class ScopeNode(string value, ScopeNode? parent)
        {
            public string Value { get; } = value;
            public ScopeNode? Parent { get; } = parent;
        }

        private sealed class ScopeDisposable(ScopeNode? previous) : IDisposable
        {
            public void Dispose() => ScopeStack.Value = previous;
        }
    }
}
