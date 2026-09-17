using CoreRentalNet.Modules.Workspace.Application.Contracts.Suggestion;

namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// How many candidates a customer is shown, decided where the identity is read.
/// </summary>
/// <remarks>
/// <para>
/// Three for an account that holds the power permission, and one for anybody else entitled to the
/// section - chosen at the middle, because a single candidate should be the neutral one rather than the
/// cheapest available or the most expensive. That is a decision about what to offer, not about what to
/// hide, and it is made here rather than in the markup: a candidate that is in the document and styled
/// away is still a control, and would still be reachable.
/// </para>
/// <para>
/// A run that composed fewer than three is left alone. The application never pads a result, so a customer
/// whose run returned two candidates sees two, and one who is entitled to one sees one of those two.
/// </para>
/// </remarks>
internal static class CandidateChoices
{
    public static IReadOnlyList<SuggestedOption> Visible(IReadOnlyList<SuggestedOption> options, bool entitledToThree)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (entitledToThree || options.Count <= 1)
        {
            return options;
        }

        // The middle, or the lower of the two middles when the count is even: never the cheapest, and
        // never the dearest.
        return [options[(options.Count - 1) / 2]];
    }
}
