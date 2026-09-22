namespace CoreRentalNet.Host.Helper;

/// <summary>
/// The catalogue's filter vocabulary: which words a filter accepts, and what a caller is told when it
/// sends one that is not among them.
/// </summary>
/// <remarks>
/// Shared by the binder that reads the query string and by the tests, because it is the contract
/// rather than a step in the request. An agent asks with words, so an unknown filter is refused with
/// the words that would have worked rather than answered with an empty list. Matching ignores letter
/// case and surrounding space; whether a filter may be left out is the binder's decision, not this
/// one's, because "not sent" and "not a word" are different answers.
/// </remarks>
public static class CatalogApiParameterHelper
{
    /// <summary>The refusal: the word that was rejected, and the words that would have worked.</summary>
    public static string Refusal(string field, string? value, IReadOnlyList<string> allowed)
        => $"Unknown {field} '{value?.Trim()}'. Use one of: {string.Join(", ", allowed)}.";
}
