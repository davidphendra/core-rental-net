namespace CoreRentalNet.Modules.Discovery.Application.Shortlist;

/// <summary>
/// One product the shortlist carries: which one, which bucket it came from, and how near the request it was.
/// </summary>
/// <remarks>
/// <para>
/// It carries <b>the SKU and nothing else about the product</b>. A name, a price and an image already have one
/// owner — the catalogue module — and the application resolves them from there; copying them here would be a
/// second answer to a question the catalogue already answers.
/// </para>
/// <para>
/// <b>The bucket travels with the item on purpose.</b> "Two per bucket, fourteen in all" is the property the
/// whole shortlist design rests on, and a caller that receives only a flat list cannot check it - it would have
/// to re-derive each product's bucket from the catalogue to know whether the coverage it was promised actually
/// happened. Carrying it makes the promise observable at the boundary rather than inferable behind it.
/// </para>
/// <para>
/// The score is a cosine similarity: a number for ranking, with no meaning on its own and no scale worth
/// publishing. It is here so that a caller can order or filter; nothing may treat it as a probability, and
/// nothing may treat it as stable across models.
/// </para>
/// </remarks>
public sealed record ShortlistItem(string Sku, CatalogBucket Bucket, float Score);
