using System.Text.Json.Serialization;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>The provider's typed answer: candidates, or the typed not-a-workspace verdict.</summary>
/// <remarks>
/// The provider's own wire shape, read where an object carries a <c>status</c>. The run's cost arrives in a
/// separate object and is merged in by the adapter rather than here.
/// </remarks>
internal sealed record MicrosoftFoundrySuggestionAgentResponse(
    [property: JsonPropertyName("status")] MicrosoftFoundrySuggestionAgentStatus Status,
    [property: JsonPropertyName("reason")] string? Reason,
    [property: JsonPropertyName("options")] IReadOnlyList<MicrosoftFoundrySuggestionAgentOption> Options);
