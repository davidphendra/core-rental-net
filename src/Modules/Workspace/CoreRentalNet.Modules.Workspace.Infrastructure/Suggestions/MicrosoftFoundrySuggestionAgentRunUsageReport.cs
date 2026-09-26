using System.Text.Json.Serialization;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>The run's cost, as the provider appends it to its answer.</summary>
/// <remarks>
/// A separate object rather than a field of the result, because the answer is streamed verbatim and a figure
/// added afterwards cannot be injected into an object already sent. The adapter therefore reads the result
/// where it finds a result and this where it finds a run cost, rather than assuming which came last.
/// </remarks>
internal sealed record MicrosoftFoundrySuggestionAgentRunUsageReport(
    [property: JsonPropertyName("runUsage")] MicrosoftFoundrySuggestionAgentRunUsage? RunUsage);
