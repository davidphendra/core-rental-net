using AgentFoundry.WorkspaceSuggestions.Catalogue;
using AgentFoundry.WorkspaceSuggestions.Contracts;
using AgentFoundry.WorkspaceSuggestions.Specifications;

namespace AgentFoundry.WorkspaceSuggestions.Selection;

/// <summary>
/// Composes the candidates: one read, one candidate per tier, and a disclosure for every slot that
/// could not differ.
/// </summary>
/// <remarks>
/// <para>
/// The read happens once per run and never per slot, because the catalogue is an immutable snapshot:
/// the whole of it is read and the slots are grouped locally, so the endpoint is asked once and the
/// answer is the same set for every tier.
/// </para>
/// <para>
/// A truncated answer is refused rather than tiered. The tier is the product at a slot's median
/// position, so a set that arrived short does not change the answer - it changes which answer is right,
/// and nothing downstream could tell. Measured: eight monitors give 300/400/475 and five give
/// 300/350/400.
/// </para>
/// </remarks>
public sealed class Suggestor(ICatalogueReader catalogue) : ISelectCandidates
{
    /// <inheritdoc />
    public int CatalogueReads { get; private set; }

    public async Task<Selection> SelectAsync(
        Specification specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        CatalogueReads++;
        var page = await catalogue.ReadAsync(cancellationToken);

        if (page.Truncated)
        {
            throw new IncompleteCatalogueException(
                $"The catalogue answered with {page.Count} of {page.Total} products, so a slot's candidates "
                + "are not the whole set and no tier can be trusted.");
        }

        var candidates = Candidates(page, specification);
        var pinned = candidates.Where(slot => TierPicker.IsPinned(slot.Value)).Select(slot => slot.Key).ToArray();

        // Every slot pinned means the three tiers are the same composition, so one candidate is sent
        // rather than three identical ones. The middle is the one offered, being neither extreme.
        if (pinned.Length == specification.Slots.Count)
        {
            return Selection([Option(specification, candidates, pinned, position: 1)], candidates);
        }

        return Selection(
            [.. Tier.All.Select((_, position) => Option(specification, candidates, pinned, position))],
            candidates);
    }

    /// <summary>The options, and the ordered SKUs they came from, as the reviewer's receipt.</summary>
    private static Selection Selection(
        IReadOnlyList<SuggestionOption> options,
        Dictionary<string, IReadOnlyList<CatalogueItem>> candidates)
        => new(
            options,
            candidates.ToDictionary(
                slot => slot.Key,
                slot => (IReadOnlyList<string>)[.. slot.Value.Select(item => item.Sku)],
                StringComparer.OrdinalIgnoreCase));

    /// <summary>A slot's candidates in tier order, or a failure naming the slot the catalogue cannot fill.</summary>
    private static Dictionary<string, IReadOnlyList<CatalogueItem>> Candidates(
        CataloguePage page,
        Specification specification)
    {
        var candidates = new Dictionary<string, IReadOnlyList<CatalogueItem>>(StringComparer.OrdinalIgnoreCase);

        foreach (var requirement in specification.Slots)
        {
            var ordered = TierPicker.Ordered(SlotCatalogue.For(page, requirement.Slot));

            if (ordered.Count == 0)
            {
                throw new IncompleteCatalogueException(
                    $"The catalogue holds no {requirement.Slot} product, so the slot cannot be filled.");
            }

            candidates[requirement.Slot] = ordered;
        }

        return candidates;
    }

    /// <summary>One candidate: every slot filled at one tier's position.</summary>
    private static SuggestionOption Option(
        Specification specification,
        Dictionary<string, IReadOnlyList<CatalogueItem>> candidates,
        IReadOnlyList<string> pinned,
        int position)
    {
        var lines = specification.Slots
            .Select(requirement => new OptionLine(
                requirement.Slot,
                TierPicker.At(candidates[requirement.Slot], position).Sku,
                requirement.Quantity))
            .ToArray();

        var chosen = lines.ToDictionary(
            line => line.Slot,
            line => candidates[line.Slot].Single(item => item.Sku == line.Sku),
            StringComparer.OrdinalIgnoreCase);

        var criteria = CriteriaMapper.For(specification.Query, lines, chosen);

        return new SuggestionOption(
            Tier.All[position],
            lines,
            criteria.Matched,
            criteria.Unevaluated,
            pinned);
    }
}
