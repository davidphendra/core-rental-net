namespace CoreRentalNet.Modules.Workspace.Application.Contracts.Suggestion;

/// <summary>What the application answers with, after checking everything the agent said.</summary>
/// <param name="Status">One of <see cref="SuggestionStatus"/>; <c>unavailable</c> is the application's own.</param>
/// <param name="Code">
/// Why the request was refused, when it was. A code rather than a sentence: the page owns the words,
/// so the same refusal reads the same way wherever it is shown.
/// </param>
/// <param name="DroppedSkus">
/// Products the agent named that the catalogue does not hold. Reported rather than hidden: an option
/// quietly missing a line is a candidate the customer was promised and cannot have.
/// </param>
public sealed record WorkspaceSuggestion(
    string Status,
    IReadOnlyList<SuggestedOption> Options,
    string? Code,
    IReadOnlyList<string> DroppedSkus);
