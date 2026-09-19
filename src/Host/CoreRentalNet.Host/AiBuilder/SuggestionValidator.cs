using CoreRentalNet.Host.Agents;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Rules;

namespace CoreRentalNet.Host.AiBuilder;

/// <summary>The trust boundary: where an agent's answer is checked before a customer sees any of it.</summary>
/// <remarks>
/// <para>
/// The agent names SKUs; the application owns truth. Between them sits this: <b>all or nothing</b> - one line
/// that cannot be honoured fails the whole run rather than being quietly dropped, because a partially
/// honoured suggestion is a lie about the price.
/// </para>
/// <para>
/// Nothing the agent said about a product survives. It names a SKU, a quantity and a purpose; the name and
/// every amount are read from the catalogue here, so a candidate cannot quote a price the catalogue does not
/// charge.
/// </para>
/// </remarks>
internal sealed class SuggestionValidator(
    IProductCatalog catalogue,
    WorkspaceSlotSettings slots,
    SuggestionSpread spread)
{
    /// <summary>Checks a whole answer, and yields the candidates that may be shown.</summary>
    /// <returns><c>false</c> when any line of it cannot be honoured, in which case there is nothing to show.</returns>
    public bool TryValidate(AgentSuggestionResult result, out IReadOnlyList<SuggestionCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(result);

        candidates = [];

        // A run that says it suggested something and shows nothing is not a suggestion.
        if (result.Options.Count == 0)
        {
            return false;
        }

        var valid = new List<SuggestionCandidate>(result.Options.Count);

        foreach (var option in result.Options)
        {
            if (!TryRead(option, out var candidate))
            {
                return false;
            }

            valid.Add(candidate!);
        }

        // Sorted by what the catalogue charges for it, so the order a customer sees is the application's
        // rather than the order the model happened to write them in.
        candidates = Label(Spread([.. valid.OrderBy(candidate => candidate.MonthlyTotal)]));

        return true;
    }

    /// <summary>One option, or nothing when a single line of it cannot be honoured.</summary>
    private bool TryRead(AgentSuggestionOption option, out SuggestionCandidate? candidate)
    {
        candidate = null;

        if (option.Lines.Count == 0)
        {
            return false;
        }

        var lines = new List<SuggestionCandidateLine>(option.Lines.Count);

        foreach (var line in option.Lines)
        {
            if (!TryPrice(line, out var priced))
            {
                return false;
            }

            lines.Add(priced!);
        }

        candidate = new SuggestionCandidate(
            string.Empty,
            lines.Sum(line => line.LineTotal),
            option.Rationale,
            lines);

        return true;
    }

    /// <summary>One line, priced from the catalogue, or nothing when it cannot be honoured.</summary>
    private bool TryPrice(AgentSuggestionLine line, out SuggestionCandidateLine? priced)
    {
        priced = null;

        var product = catalogue.Find(line.Sku);

        // Four ways a line cannot be honoured, and each is a real one:
        //   - the catalogue does not hold the SKU, so there is no name and no price for it;
        //   - the quantity is below one, which nothing upstream rejects - the framework's deserializer accepts
        //     a zero silently, and the schema that forbids it is not enforced on this path;
        //   - the quantity is over what the slot accepts;
        //   - the product does not belong in the slot it was put in.
        if (product is null
            || line.Quantity < 1
            || line.Quantity > slots.CapacityFor(line.Slot)
            || CatalogSlotMapping.SlotFor(product.Category, product.SubCategory) != line.Slot)
        {
            return false;
        }

        priced = new SuggestionCandidateLine(
            product.Sku,
            product.Name,
            line.Quantity,
            product.MonthlyPrice.Amount * line.Quantity);

        return true;
    }

    /// <summary>
    /// The options that may be shown: fewer than were given when they cannot be shown as a range.
    /// </summary>
    /// <remarks>
    /// The rule is on the <b>range only</b> - the dearest against the cheapest - so a middle option carries no
    /// second margin. That is not a simplification: a middle margin can only ever refuse an option the range
    /// had already accepted.
    ///
    /// When the range fails, fewer are shown, and <b>fewer means one</b>. That is provable rather than a
    /// choice: no subset of two or more can satisfy a rule the whole set fails, because dropping the cheapest
    /// raises the minimum and dropping the dearest lowers the maximum, so either makes the range narrower.
    /// What is shown is then the cheapest, which is the honest end when no range could be offered.
    /// </remarks>
    private IReadOnlyList<SuggestionCandidate> Spread(IReadOnlyList<SuggestionCandidate> sorted)
        => sorted.Count > 1 && !spread.Holds(sorted[0].MonthlyTotal, sorted[^1].MonthlyTotal)
            ? [sorted[0]]
            : sorted;

    /// <summary>Where each shown candidate sits in the range, assigned by rank.</summary>
    private static IReadOnlyList<SuggestionCandidate> Label(IReadOnlyList<SuggestionCandidate> shown)
        => [.. shown.Select((candidate, rank) => candidate with { Label = LabelFor(rank) })];

    /// <remarks>
    /// A function rather than a list to index, so a fourth option cannot be an out-of-range exception. The
    /// contract bounds a result at three, and this path does not enforce that - recorded rather than guarded
    /// by a rule the story does not ask for.
    /// </remarks>
    private static string LabelFor(int rank) => rank switch
    {
        0 => "Budget",
        1 => "Balanced",
        _ => "Premium",
    };
}
