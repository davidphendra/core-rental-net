using Microsoft.Extensions.Logging;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// Records what the application logs, so a test can assert the line a request writes.
/// </summary>
/// <remarks>
/// Registered as an <see cref="ILoggerProvider"/> in a test host. Entries carry the category, the
/// level and the structured properties, so a test can look at the fields rather than only the text.
/// </remarks>
internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly List<CapturedEntry> _entries = [];

    public IReadOnlyList<CapturedEntry> Entries => _entries;

    /// <summary>Forgets what has been logged, so a test can look only at its own request.</summary>
    public void Clear() => _entries.Clear();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, _entries);

    public void Dispose()
    {
    }

    internal sealed record CapturedEntry(
        string Category,
        LogLevel Level,
        string Message,
        IReadOnlyList<KeyValuePair<string, object?>>? Properties,
        Exception? Exception = null)
    {
        /// <summary>The value of one structured property, or null when it is absent or null.</summary>
        public object? Property(string name)
            => Properties?.FirstOrDefault(pair => pair.Key == name).Value;
    }

    private sealed class CapturingLogger(string category, List<CapturedEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => entries.Add(new CapturedEntry(
                category,
                logLevel,
                formatter(state, exception),
                state as IReadOnlyList<KeyValuePair<string, object?>>,
                exception));
    }
}
