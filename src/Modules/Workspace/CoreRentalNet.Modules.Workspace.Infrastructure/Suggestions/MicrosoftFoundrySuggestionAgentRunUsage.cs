using System.Text.Json.Serialization;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>What the run cost, as the provider reported it.</summary>
internal sealed record MicrosoftFoundrySuggestionAgentRunUsage(
    [property: JsonPropertyName("modelCalls")] int ModelCalls,
    [property: JsonPropertyName("inputTokens")] int InputTokens,
    [property: JsonPropertyName("outputTokens")] int OutputTokens,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("promptVersion")] string PromptVersion);
