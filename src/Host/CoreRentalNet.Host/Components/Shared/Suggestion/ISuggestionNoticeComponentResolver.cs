namespace CoreRentalNet.Host.Components.Shared.Suggestion;

/// <summary>Which component draws which notice, so the model never names a component itself.</summary>
internal interface ISuggestionNoticeComponentResolver
{
    Type ResolveComponentType(SuggestionNoticeKind suggestionNoticeKind);
}
