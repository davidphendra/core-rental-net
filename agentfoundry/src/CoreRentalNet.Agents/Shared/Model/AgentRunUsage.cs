using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Shared.Model;

/// <summary>What a run cost, as the agent observed it, in the shape the application records.</summary>
/// <remarks>
/// The figures are counted by the agent, which is the only party that sees its own model calls: a model cannot
/// report how many times it was called, so asking it would be asking the one party unable to answer.
/// </remarks>
public sealed record AgentRunUsage(
    [property: JsonPropertyName("modelCalls")] int ModelCalls,
    [property: JsonPropertyName("inputTokens")] int InputTokens,
    [property: JsonPropertyName("outputTokens")] int OutputTokens,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("promptVersion")] string PromptVersion);
