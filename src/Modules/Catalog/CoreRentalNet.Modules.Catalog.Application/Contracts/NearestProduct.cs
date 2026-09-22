namespace CoreRentalNet.Modules.Catalog.Application.Contracts;

/// <summary>
/// One product found by a vector search, with how far its stored text is from the request.
/// </summary>
/// <remarks>
/// <para>
/// <b>The distance is sqlite-vec's, and nothing here recomputes it.</b> It is a cosine distance: <c>0</c> means
/// the two vectors point the same way and a larger number means farther, so a caller that orders products
/// orders them ascending. The number has no meaning on its own and no scale worth publishing; it is here to
/// order by, not to show.
/// </para>
/// <para>
/// It carries the SKU and the distance and nothing else, because those are the only two things the search
/// yields: which product, and how far. A name or a price already has one owner — the catalogue module — and
/// the application resolves those from there.
/// </para>
/// </remarks>
/// <param name="Sku">The product the distance belongs to.</param>
/// <param name="Distance">How far the product's stored text is from the request, as sqlite-vec measured it.</param>
public sealed record NearestProduct(string Sku, double Distance);
