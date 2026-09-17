using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace AgentFoundry.WorkspaceSuggestions.Observability;

/// <summary>
/// One line and one span per run, so a run can be read afterwards and found while it is happening.
/// </summary>
/// <remarks>
/// The span exists so that a run has a shape in a trace - one parent with a child per stage - which is
/// what makes a loop that spent three attempts visible at a glance rather than by reading timestamps.
/// The logger category is named so a test can find the line.
/// </remarks>
internal static class RunLog
{
    /// <summary>The logger category the line is written under, so a test can find it.</summary>
    public const string Category = "AgentFoundry.WorkspaceSuggestions.Observability";

    /// <summary>The activity source a run's spans are published under.</summary>
    public static readonly ActivitySource Source = new("AgentFoundry.WorkspaceSuggestions");

    public static void Completed(ILogger logger, RunRecord record)
        => logger.LogInformation(
            "Run {RequestId} ended {Outcome} after {Attempts} attempt(s): slots inferred={Inferred}, "
            + "findings={Findings}, catalogue reads={Reads}, request length={QueryLength}.",
            record.RequestId,
            record.Outcome,
            record.Attempts,
            record.SlotsInferred,
            record.Findings,
            record.CatalogueReads,
            record.QueryLength);
}
