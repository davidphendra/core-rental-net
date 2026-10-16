using Spectre.Console;

namespace CoreRentalNet.CatalogPopulate.Reporting;

/// <summary>Renders one progress bar per step with Spectre.Console, and owns the live run's display.</summary>
/// <remarks>
/// A step appears the first time it reports and keeps its bar once finished, so the run's steps accumulate
/// rather than replace each other. The percentage and remaining time are the library's, not this type's: it
/// owns only the step-to-bar map and the context the pipeline runs inside. The description is bounded here so
/// a narrow terminal still has room for the bar and the numbers.
/// </remarks>
internal sealed class SpectreProgressReporter : IProgressReporter
{
    private const int BarWidth = 20;
    private const int ReservedWidth = 48;

    private readonly Dictionary<string, ProgressTask> _tasks = new(StringComparer.Ordinal);
    private ProgressContext? _context;
    private string? _summary;

    /// <summary>Runs the pipeline inside the live display, returning the pipeline's exit code.</summary>
    public async Task<int> RunAsync(Func<Task<int>> pipeline)
    {
        ArgumentNullException.ThrowIfNull(pipeline);

        var code = await AnsiConsole.Progress()
            .AutoRefresh(true)
            .Columns(
                new TaskDescriptionColumn { Wrap = false },
                new ProgressBarColumn { Width = BarWidth },
                new PercentageColumn(),
                new ElapsedTimeColumn(),
                new RemainingTimeColumn())
            .StartAsync(async context =>
            {
                _context = context;
                return await pipeline();
            });

        // Written after the display is down, so the line does not land in the middle of a live bar.
        if (_summary is not null)
        {
            AnsiConsole.WriteLine(_summary);
        }

        return code;
    }

    /// <summary>Holds the closing line until the live display is taken down.</summary>
    public void Complete(string summary) => _summary = summary;

    public void Report(ProgressUpdate update)
    {
        if (_context is null)
        {
            return;
        }

        var description = Markup.Escape(Describe(update.Step, update.Detail));

        if (!_tasks.TryGetValue(update.Step, out var task))
        {
            task = _context.AddTask(description, autoStart: true, maxValue: Math.Max(1L, update.Total));
            _tasks[update.Step] = task;
        }

        task.MaxValue = Math.Max(1L, update.Total);
        task.Value = update.Completed;
        task.Description = description;

        _context.Refresh();
    }

    /// <summary>The step and what it is doing now, cut to the width the fixed columns leave.</summary>
    private static string Describe(string step, string detail)
    {
        var text = string.IsNullOrWhiteSpace(detail) ? step : $"{step} · {detail}";

        // A redirected run has no terminal to overflow, and its lines are read in full from a log.
        if (!AnsiConsole.Profile.Capabilities.Interactive)
        {
            return text;
        }

        var room = Math.Max(24, AnsiConsole.Profile.Width - ReservedWidth);

        return text.Length <= room ? text : string.Concat(text.AsSpan(0, room - 1), "…");
    }
}
