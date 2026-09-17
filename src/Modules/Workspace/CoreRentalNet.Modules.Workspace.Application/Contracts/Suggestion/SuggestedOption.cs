namespace CoreRentalNet.Modules.Workspace.Application.Contracts.Suggestion;

/// <summary>One candidate, ready to be shown: every SKU in it exists and every price is the catalogue's.</summary>
/// <param name="Unevaluated">
/// The criteria the catalogue could not express, as the customer's own words. Shown rather than
/// dropped, so an option never looks as though it answered something it did not.
/// </param>
/// <param name="PinnedSlots">
/// Slots too shallow for the tiers to differ, where the same product fills every candidate and the tier
/// therefore means nothing for that slot.
/// </param>
public sealed record SuggestedOption(
    string Tier,
    IReadOnlyList<SuggestedLine> Lines,
    IReadOnlyList<string> Criteria,
    IReadOnlyList<string> Unevaluated,
    IReadOnlyList<string> PinnedSlots);
