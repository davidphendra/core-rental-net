using System.Text.Json.Serialization;

namespace CoreRentalNet.Host.Agents;

/// <summary>The run's cost, as the agent appends it to its answer.</summary>
/// <remarks>
/// A separate object rather than a field of the result, because the model's answer is streamed verbatim and
/// a figure added afterwards cannot be injected into an object already sent. The application therefore reads
/// the result where it finds a result and this where it finds a run cost, rather than assuming which came
/// last — the answer also carries the rephraser's specification, so "the last object" was never a safe rule
/// and is no longer the rule.
/// </remarks>
internal sealed record AgentRunUsageReport(
    [property: JsonPropertyName("runUsage")] AgentRunUsage? RunUsage);
