using System.Text.Json.Serialization;

namespace CoreRentalNet.Host.Agents;

/// <summary>One candidate setup as the agent stated it: the lines and its one-paragraph rationale.</summary>
/// <remarks>
/// Carries no price and no label, by contract. The application resolves every amount from the catalogue and
/// labels the candidates by rank, so neither can come from here.
/// </remarks>
internal sealed record AgentSuggestionOption(
    [property: JsonPropertyName("lines")] IReadOnlyList<AgentSuggestionLine> Lines,
    [property: JsonPropertyName("rationale")] string Rationale);
