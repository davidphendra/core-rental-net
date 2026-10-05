using System.Text.Json.Serialization;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>One candidate setup as the agent stated it: the lines it fills.</summary>
internal sealed record WorkspaceSuggestionAgentOption(
    [property: JsonPropertyName("lines")] IReadOnlyList<WorkspaceSuggestionAgentLine> Lines);
