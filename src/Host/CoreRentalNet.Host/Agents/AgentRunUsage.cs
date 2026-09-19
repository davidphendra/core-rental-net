using System.Text.Json.Serialization;

namespace CoreRentalNet.Host.Agents;

/// <summary>What the run cost, as the agent reported it.</summary>
/// <remarks>
/// Recorded on the run rather than guessed at, and the model call count is the field that makes the "two
/// calls" assumption visible if it ever becomes three.
/// </remarks>
internal sealed record AgentRunUsage(
    [property: JsonPropertyName("modelCalls")] int ModelCalls,
    [property: JsonPropertyName("inputTokens")] int InputTokens,
    [property: JsonPropertyName("outputTokens")] int OutputTokens,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("promptVersion")] string PromptVersion);
