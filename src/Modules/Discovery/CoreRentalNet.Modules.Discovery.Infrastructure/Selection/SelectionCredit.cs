using CoreRentalNet.Modules.Discovery.Application.Selection;

namespace CoreRentalNet.Modules.Discovery.Infrastructure.Selection;

/// <summary>
/// Credits the products a customer applied.
/// </summary>
/// <remarks>
/// <para>
/// <b>The same product named in two slots is credited once</b>: the slot capacity allows three monitors, and
/// three of the same monitor is still one decision about one product.
/// </para>
/// <para>
/// <b>It never touches the offered count.</b> The denominator belongs to the retrieval path; a credit that also
/// incremented it would make a product chosen once read as a perfect hundred per cent, which is exactly the
/// drift the ratio exists to prevent.
/// </para>
/// </remarks>
public sealed class SelectionCredit(
    DiscoveryContext context,
    SelectionSettings settings,
    TimeProvider clock) : ICreditSelection
{
    /// <inheritdoc />
    public async Task<int> CreditAsync(IReadOnlyList<string> skus, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(skus);

        var credited = skus.Distinct(StringComparer.Ordinal).ToArray();

        if (credited.Length == 0)
        {
            return 0;
        }

        var rows = new SelectionRows(context, settings);
        await rows.LoadAsync(credited, cancellationToken).ConfigureAwait(false);

        var now = clock.GetUtcNow();

        foreach (var sku in credited)
        {
            rows.Touch(sku, now).DecayedChosen += 1;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return credited.Length;
    }
}
