using System.Text.Json.Serialization;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>One retry the agent streamed, as its schema spells it.</summary>
internal sealed record MicrosoftFoundrySuggestionRetryAttempt(
    [property: JsonPropertyName("nextAttemptNumber")] int NextAttemptNumber,
    [property: JsonPropertyName("maximumAttemptCount")] int MaximumAttemptCount);
