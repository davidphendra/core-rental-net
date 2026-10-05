namespace CoreRentalNet.Host.Components.Shared.Suggestion;

/// <summary>The one place a notice kind becomes a component, so a designer changes the view and not the run.</summary>
internal sealed class SuggestionNoticeComponentResolver : ISuggestionNoticeComponentResolver
{
    private static readonly IReadOnlyDictionary<SuggestionNoticeKind, Type> NoticeComponents =
        new Dictionary<SuggestionNoticeKind, Type>
        {
            [SuggestionNoticeKind.Stage] = typeof(SuggestionStageNoticeView),
            [SuggestionNoticeKind.Outcome] = typeof(SuggestionOutcomeNoticeView),
            [SuggestionNoticeKind.Failure] = typeof(SuggestionFailureNoticeView),
            [SuggestionNoticeKind.Stopped] = typeof(SuggestionStoppedNoticeView),
        };

    public Type ResolveComponentType(SuggestionNoticeKind suggestionNoticeKind)
        => NoticeComponents[suggestionNoticeKind];
}
