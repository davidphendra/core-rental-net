using CoreRentalNet.Modules.Discovery.Application.Selection;

namespace CoreRentalNet.Modules.Discovery.Infrastructure.Selection;

/// <summary>
/// Records the products a run showed to the model.
/// </summary>
/// <remarks>
/// <b>It is the only thing that writes an offered count</b>, and it writes it once per run rather than once per
/// product per event: the caller decides that the request reached the model, and this class decides nothing about
/// when that was. A distinctly-named product is one offer however many slots it was considered for.
/// </remarks>
public sealed class SelectionOffers(
    DiscoveryContext context,
    SelectionSettings settings,
    TimeProvider clock) : IOfferSelection
{
    /// <inheritdoc />
    public async Task<int> OfferAsync(IReadOnlyList<string> skus, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(skus);

        var offered = skus.Distinct(StringComparer.Ordinal).ToArray();

        if (offered.Length == 0)
        {
            return 0;
        }

        var rows = new SelectionRows(context, settings);
        await rows.LoadAsync(offered, cancellationToken).ConfigureAwait(false);

        var now = clock.GetUtcNow();

        foreach (var sku in offered)
        {
            rows.Touch(sku, now).DecayedOffered += 1;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return offered.Length;
    }
}
