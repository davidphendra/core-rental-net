using System.Globalization;
using System.Text.RegularExpressions;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;

/// <summary>Reads a budget the customer actually wrote, rather than one a model inferred.</summary>
/// <remarks>
/// <para>
/// The rephraser is told that a figure in a retry correction is not a total. A model can still treat it as one,
/// so the budget invariant needs a source of truth that no model supplied. That source is the customer sentence.
/// </para>
/// <para>
/// Only a number with a budget cue counts: a currency marker, or a phrase such as "under", "below", "up to" or
/// "budget". A screen size or a refresh rate therefore cannot become a monthly ceiling.
/// </para>
/// </remarks>
public static class WorkspaceCustomerBudgetReader
{
    private static readonly Regex[] s_budgetPatterns =
    [
        new(
            @"\b(?:under|below|less\s+than|up\s+to|max(?:imum)?|budget(?:\s+of)?)\s*"
            + @"(?:idr|rp\.?\s*)?(?<amount>\d[\d.,]*)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant),
        new(
            @"\b(?:idr|rp\.?\s*)\s*(?<amount>\d[\d.,]*)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant),
        new(
            @"\b(?<amount>\d[\d.,]*)\s*(?:idr|rp)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant),
    ];

    /// <summary>The first budget the sentence states, or false when it states none.</summary>
    public static bool TryRead(string customerSentence, out int amount)
    {
        foreach (var pattern in s_budgetPatterns)
        {
            var match = pattern.Match(customerSentence);
            if (match.Success
                && int.TryParse(
                    match.Groups["amount"].Value.Replace(".", string.Empty, StringComparison.Ordinal)
                        .Replace(",", string.Empty, StringComparison.Ordinal),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out amount)
                && amount > 0)
            {
                return true;
            }
        }

        amount = 0;
        return false;
    }
}
