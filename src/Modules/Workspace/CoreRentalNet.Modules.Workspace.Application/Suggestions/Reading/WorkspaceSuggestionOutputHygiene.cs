using System.Text.RegularExpressions;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Reading;

/// <summary>Makes a completed narrative field fit to show, before it is shown.</summary>
/// <remarks>
/// <para>
/// The prompts already forbid a price and a product name, so this is the <b>deterministic backstop</b> rather
/// than the only guard. It runs on a complete value - the reader guarantees that - which is why a URL split
/// across two streamed chunks cannot escape the strip.
/// </para>
/// <para>
/// It is an ordinary class the run calls, not framework middleware, because it has to run where the display
/// happens rather than somewhere else that has to be trusted to have run. It touches free text and nothing
/// else: no SKU, no quantity and no slot passes through here.
/// </para>
/// </remarks>
public static class WorkspaceSuggestionOutputHygiene
{
    /// <summary>An option's rationale, which carries the argument for a whole setup.</summary>
    public const int RationaleCap = 280;

    /// <summary>One line's <c>why</c>, which is a phrase rather than a paragraph.</summary>
    public const int WhyCap = 140;

    private static readonly Regex Url = new(@"\b(?:https?://|www\.)\S+", RegexOptions.Compiled);

    private static readonly Regex Markup = new(@"<[^>]*>", RegexOptions.Compiled);

    private static readonly Regex MarkdownLink = new(@"\[([^\]]*)\]\([^)]*\)", RegexOptions.Compiled);

    private static readonly Regex Currency = new(
        @"(?:Rp|IDR|USD|EUR|GBP|SGD|MYR)\s?\d[\d.,]*|[$€£¥]\s?\d[\d.,]*|\d[\d.,]*\s?(?:Rp|IDR|USD|EUR|GBP|SGD|MYR)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    /// <summary>Stripping a tag or an amount leaves a space stranded before the punctuation.</summary>
    private static readonly Regex StrandedSpace = new(@"\s+([.,;:!?])", RegexOptions.Compiled);

    public static string Apply(WorkspaceSuggestionNarrativeFieldKind kind, string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        // A tag becomes a space rather than nothing, so "A<b>tidy" does not fuse into "Atidy"; the space
        // it leaves before a full stop is removed afterwards.
        var stripped = Url.Replace(text, " ");
        stripped = MarkdownLink.Replace(stripped, "$1");
        stripped = Markup.Replace(stripped, " ");
        stripped = Currency.Replace(stripped, " ");
        stripped = Whitespace.Replace(stripped, " ");
        stripped = StrandedSpace.Replace(stripped, "$1").Trim();

        return Truncate(
            stripped,
            kind is WorkspaceSuggestionNarrativeFieldKind.Rationale ? RationaleCap : WhyCap);
    }

    /// <summary>Cuts to the cap at a word boundary, marking the cut, and never exceeds the cap.</summary>
    private static string Truncate(string text, int cap)
    {
        if (text.Length <= cap)
        {
            return text;
        }

        // One character is held back for the mark, so the result is at most the cap.
        var head = text[..(cap - 1)];
        var lastSpace = head.LastIndexOf(' ');
        var cut = lastSpace > 0 ? head[..lastSpace] : head;

        return cut.TrimEnd() + "…";
    }
}
