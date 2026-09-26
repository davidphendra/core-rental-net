using System.Text.Json.Serialization;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>One candidate setup as the provider stated it: the lines and its one-paragraph rationale.</summary>
internal sealed record MicrosoftFoundrySuggestionAgentOption(
    [property: JsonPropertyName("lines")] IReadOnlyList<MicrosoftFoundrySuggestionAgentLine> Lines,
    [property: JsonPropertyName("rationale")] string Rationale);
