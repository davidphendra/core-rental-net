namespace CoreRentalNet.Host.Components.Shared.Suggestion;

/// <summary>Which kind of notice a run has produced, and the key the component resolver is asked with.</summary>
internal enum SuggestionNoticeKind
{
    Stage,
    Narrative,
    Outcome,
    Failure,
    Stopped,
}
