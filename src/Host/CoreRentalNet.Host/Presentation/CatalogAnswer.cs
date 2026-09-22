using CoreRentalNet.Host.Controllers;
using CoreRentalNet.Host.Enums;
using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// The shape a catalogue answer takes: the published view, or the compact projection a caller asked
/// for.
/// </summary>
/// <remarks>
/// The two projections are different envelope types, so which one an answer is cannot be expressed as
/// a single return type - the caller writes whatever comes back. Naming the choice here keeps the
/// controller about the request, beside <see cref="CompactCatalogProjection"/>, which is about the
/// compact shape itself.
/// </remarks>
internal static class CatalogAnswer
{
    /// <param name="page">The rows this answer carries.</param>
    /// <param name="matched">How many rows matched, which is more than the page when it was capped.</param>
    /// <param name="projection">The fields the caller asked for.</param>
    /// <param name="catalogue">The whole catalogue, which the compact envelope states its currency from.</param>
    public static object Of(
        IReadOnlyList<ProductView> page,
        int matched,
        CatalogProjection projection,
        IReadOnlyList<ProductView> catalogue)
        => projection == CatalogProjection.Compact
            ? CompactCatalogProjection.Of(page, matched, CompactCatalogProjection.CurrencyOf(catalogue))
            : new ApiCollection<ProductView>(page, page.Count, matched);

    /// <summary>The rows an answer carries: all that matched, or the first <paramref name="limit"/>.</summary>
    /// <remarks>
    /// Here rather than in the endpoint, because the endpoint and the MCP tools both cap an answer and only
    /// this layer knows how: two copies of the cap is how the two surfaces come to truncate differently.
    /// </remarks>
    public static IReadOnlyList<ProductView> Cap(IReadOnlyList<ProductView> matched, int limit)
        => matched.Count <= limit ? matched : [.. matched.Take(limit)];

    /// <summary>The compact envelope both the catalogue API and the tools publish.</summary>
    public static CompactCatalogCollection Compact(
        IReadOnlyList<ProductView> matched,
        int limit,
        IReadOnlyList<ProductView> catalogue)
        => CompactCatalogProjection.Of(
            Cap(matched, limit),
            matched.Count,
            CompactCatalogProjection.CurrencyOf(catalogue));
}
