using System.Diagnostics;

namespace CoreRentalNet.Agents.Shared.Telemetry;

/// <summary>Finds the run an activity belongs to, even when the current activity does not carry the tag.</summary>
/// <remarks>
/// The model and tool clients do not always run directly under the workflow node: framework activities can sit
/// between them. Walking the parent chain keeps those spans attributable without making a feature know about
/// evaluation or telemetry.
/// </remarks>
internal static class RunIdResolver
{
    public static string? Resolve(Activity? activity)
    {
        for (var current = activity; current is not null; current = current.Parent)
        {
            if (current.GetTagItem(WorkspaceTelemetry.RunId) is string runId
                && !string.IsNullOrWhiteSpace(runId))
            {
                return runId;
            }
        }

        return null;
    }
}
