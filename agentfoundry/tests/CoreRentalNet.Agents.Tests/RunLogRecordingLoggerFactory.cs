using Microsoft.Extensions.Logging;

namespace CoreRentalNet.Agents.Tests;

/// <summary>A logger factory that keeps every line a run writes, so the lines can be searched.</summary>
/// <remarks>
/// <b>Written by hand rather than substituted, because what it must preserve is the message text exactly.</b> The
/// assertion it exists for is "no log line contains this secret", and a substitute that formatted messages on its
/// own terms would be testing the substitute.
/// </remarks>
internal sealed class RunLogRecordingLoggerFactory : ILoggerFactory
{
    /// <summary>Every line written through this factory, in order, with its category.</summary>
    public List<string> RecordedLines { get; } = [];

    public ILogger CreateLogger(string categoryName) => new RunLogRecordingLogger(RecordedLines, categoryName);

    public void AddProvider(ILoggerProvider provider)
    {
        // The recorded lines are the whole purpose, so a second provider has nothing to add.
    }

    public void Dispose()
    {
        // Nothing is held that needs releasing beyond the list, which the test owns.
    }

    private sealed class RunLogRecordingLogger(List<string> recordedLines, string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => recordedLines.Add($"{categoryName}: {formatter(state, exception)}");
    }
}
