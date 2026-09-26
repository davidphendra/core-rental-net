namespace CoreRentalNet.Host.Components.Shared.Suggestion;

/// <summary>One thing the panel shows: the semantic model the wire is read into and the views are chosen from.</summary>
/// <remarks>
/// A closed hierarchy, so the list's <c>switch</c> over it is exhaustive and a new kind is a compile error at
/// the resolver rather than a notice that silently draws nothing. The model is the page's, not the agent's: the
/// agent never names a notice and never chooses a component.
/// </remarks>
public abstract record SuggestionNotice
{
    private protected SuggestionNotice()
    {
    }
}
