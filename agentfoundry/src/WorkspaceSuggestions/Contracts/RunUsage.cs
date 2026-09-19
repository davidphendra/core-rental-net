using System.ComponentModel;
using System.Text.Json.Serialization;

namespace WorkspaceSuggestions.Contracts;

/// <summary>What a run cost, so the model call count can never drift unnoticed.</summary>
/// <remarks>
/// Recorded on every run. The call count is two with native structured output on the Responses path and
/// three if the middleware fallback is needed, and it is what the evaluation tier sets the per-customer
/// cap from.
/// </remarks>
public sealed record RunUsage(
    [property: JsonPropertyName("modelCalls")]
    [property: Description("How many model calls the run made.")]
    int ModelCalls,
    [property: JsonPropertyName("inputTokens")]
    [property: Description("Input tokens billed for the run.")]
    int InputTokens,
    [property: JsonPropertyName("outputTokens")]
    [property: Description("Output tokens billed for the run.")]
    int OutputTokens,
    [property: JsonPropertyName("model")]
    [property: Description("The model deployment that answered.")]
    string Model,
    [property: JsonPropertyName("promptVersion")]
    [property: Description("The prompt version that produced the answer.")]
    string PromptVersion);
