using CoreRentalNet.Modules.Discovery.Application.Selection;
using Microsoft.EntityFrameworkCore;

namespace CoreRentalNet.Modules.Discovery.Infrastructure.Selection;

/// <summary>
/// The rows one write is about, brought forward to the moment of that write.
/// </summary>
/// <remarks>
/// <para>
/// <b>It exists so the two writes cannot decay differently.</b> Crediting and offering both have to bring a row
/// forward and then add to one of its counters, and if either did that its own way the two would eventually
/// disagree about how old a row is — which is the same as disagreeing about what the ratio means.
/// </para>
/// <para>
/// <b>Loaded once, then touched in memory.</b> One read for the whole batch, because a run offers fourteen
/// products and the alternative is fourteen round trips through a single-writer database.
/// </para>
/// </remarks>
internal sealed class SelectionRows(DiscoveryContext context, SelectionSettings settings)
{
    private readonly Dictionary<string, CatalogSelection> _known = new(StringComparer.Ordinal);

    /// <summary>Reads the rows for these products, so touching one does not hit the database.</summary>
    public async Task LoadAsync(IReadOnlyList<string> skus, CancellationToken cancellationToken)
    {
        var found = await context.Selections
            .Where(selection => skus.Contains(selection.Sku))
            .ToDictionaryAsync(selection => selection.Sku, cancellationToken)
            .ConfigureAwait(false);

        foreach (var (sku, row) in found)
        {
            _known[sku] = row;
        }
    }

    /// <summary>The row for a product, decayed to <paramref name="now"/> — created when it has no row yet.</summary>
    /// <remarks>
    /// The decay happens here and BEFORE whatever the caller adds, which is the rule the whole signal rests on:
    /// decaying afterwards would count today's event as though it were as old as the row.
    /// </remarks>
    public CatalogSelection Touch(string sku, DateTimeOffset now)
    {
        if (!_known.TryGetValue(sku, out var row))
        {
            row = new CatalogSelection { Sku = sku };
            context.Selections.Add(row);
            _known[sku] = row;
        }
        else
        {
            var multiplier = SelectionDecay.Multiplier(now - row.LastUpdatedUtc, settings.HalfLife);

            row.DecayedOffered *= multiplier;
            row.DecayedChosen *= multiplier;
        }

        row.LastUpdatedUtc = now;

        return row;
    }
}
