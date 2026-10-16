using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// The shape a catalogue answer takes.
/// </summary>
/// <remarks>
/// One shape now: the module's published <see cref="ProductDto"/> is the compact answer, so there is
/// nothing left to project it onto. Naming the answer here keeps the controller about the request.
/// </remarks>
internal static class CatalogAnswer
{
    /// <param name="page">The rows this answer carries.</param>
    /// <param name="matched">How many rows matched, which is more than the page when it was capped.</param>
    public static ApiCollection<ProductDto> Of(IReadOnlyList<ProductDto> page, int matched)
        => new(page, page.Count, matched);

    /// <summary>The rows an answer carries: all that matched, or the first <paramref name="limit"/>.</summary>
    /// <remarks>
    /// Here rather than in the endpoint, because the endpoint and the MCP tools both cap an answer and only
    /// this layer knows how: two copies of the cap is how the two surfaces come to truncate differently.
    /// </remarks>
    public static IReadOnlyList<ProductDto> Cap(IReadOnlyList<ProductDto> matched, int limit)
        => matched.Count <= limit ? matched : [.. matched.Take(limit)];
}
