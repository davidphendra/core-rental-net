using System.Text.Json.Serialization;

namespace CoreRentalNet.Host.Agents;

/// <summary>One candidate setup as the agent stated it: the lines and its one-paragraph rationale.</summary>
/// <remarks>
/// Each line carries the catalogue's own name and amount, stated by the agent from the tool result it was
/// given; the application sums the lines for the candidate's total. No band label is carried.
/// </remarks>
internal sealed record AgentSuggestionOption(
    [property: JsonPropertyName("lines")] IReadOnlyList<AgentSuggestionLine> Lines,
    [property: JsonPropertyName("rationale")] string Rationale);
