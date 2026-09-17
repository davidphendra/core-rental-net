namespace AgentFoundry.WorkspaceSuggestions.Intent;

/// <summary>
/// Reduces a customer's words to something a lookup can be done against.
/// </summary>
/// <remarks>
/// Lower case, punctuation turned into space, runs of space collapsed, and a trailing <c>s</c> dropped
/// from each word so that "monitors" and "monitor" are the same word. Deliberately mechanical: this is
/// the part that must be reproducible, and everything a person would call "understanding" happens
/// either in the table or behind a port.
/// </remarks>
internal static class QueryText
{
    private static readonly char[] Punctuation =
        ['.', ',', ';', ':', '!', '?', '"', '\'', '(', ')', '[', ']', '-', '/', '\\', '*', '+', '&'];

    public static string Normalise(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return string.Empty;
        }

        var separated = query.ToLowerInvariant();

        foreach (var mark in Punctuation)
        {
            separated = separated.Replace(mark, ' ');
        }

        var words = separated
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Singular);

        return string.Join(' ', words);
    }

    /// <summary>The word without a plural ending, so a lookup does not need both spellings.</summary>
    private static string Singular(string word)
        => word.Length > 3 && word.EndsWith('s') && !word.EndsWith("ss", StringComparison.Ordinal)
            ? word[..^1]
            : word;
}
