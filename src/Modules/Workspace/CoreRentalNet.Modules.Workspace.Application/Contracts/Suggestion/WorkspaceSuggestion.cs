namespace CoreRentalNet.Modules.Workspace.Application.Contracts.Suggestion;

/// <summary>
/// What a run answered, with everything the application checked and everything it could not.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DroppedSkus"/> is the honest half of checking: an agent that named a SKU the catalogue does
/// not hold had that line removed, and the customer is told rather than shown a candidate that is quietly
/// smaller than the agent proposed.
/// </para>
/// <para>
/// <see cref="Findings"/> is present on an exhausted run, which is a result rather than a failure: the
/// candidates travel with what was wrong with them.
/// </para>
/// </remarks>
public sealed record WorkspaceSuggestion(
    string Status,
    IReadOnlyList<SuggestedOption> Options,
    string? Code,
    IReadOnlyList<string> DroppedSkus,
    IReadOnlyList<SuggestionFinding> Findings);
