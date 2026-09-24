namespace CoreRentalNet.Host.Agents;

/// <summary>The agent's typed answer: candidates, or the typed not-a-workspace verdict.</summary>
/// <remarks>
/// <para>
/// The wire shape of <c>suggestion.result.schema.json</c> — <b>minus the run's cost</b>, which the agent
/// appends separately and the adapter merges in. That is not a convenience: the model cannot observe how
/// many calls were made or what they cost, so asking it for those numbers produced a plausible invention
/// that validation accepted.
/// </para>
/// <para>
/// <see cref="RunUsage"/> is therefore nullable here and is filled from the agent's own report. A run whose
/// cost did not arrive still has a result — the customer sees suggestions — and the missing record is the
/// application's to notice rather than a reason to withhold an answer.
/// </para>
/// </remarks>
internal sealed record AgentSuggestionResult(
    [property: System.Text.Json.Serialization.JsonPropertyName("status")] AgentSuggestionStatus Status,
    [property: System.Text.Json.Serialization.JsonPropertyName("reason")] string? Reason,
    [property: System.Text.Json.Serialization.JsonPropertyName("options")]
    IReadOnlyList<AgentSuggestionOption> Options,
    [property: System.Text.Json.Serialization.JsonIgnore] AgentRunUsage? RunUsage = null);
