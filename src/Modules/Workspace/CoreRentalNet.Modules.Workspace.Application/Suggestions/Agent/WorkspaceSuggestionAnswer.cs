namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

/// <summary>The agent's typed answer in the application's own words, before the port is crossed.</summary>
/// <remarks>
/// The adapter maps the provider's JSON onto this, so nothing downstream of the port ever sees a provider
/// type. <see cref="RunUsage"/> is nullable because a run whose cost did not arrive still has an answer - the
/// customer sees candidates, and the missing record is the application's to notice rather than a reason to
/// withhold the answer.
/// </remarks>
public sealed record WorkspaceSuggestionAnswer(
    WorkspaceSuggestionAnswerStatus Status,
    IReadOnlyList<WorkspaceSuggestionCandidate> Candidates,
    WorkspaceSuggestionRunUsage? RunUsage);
