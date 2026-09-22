namespace CoreRentalNet.BuildingBlocks.Application.Embeddings;

/// <summary>
/// What a set of stored vectors was built from.
/// </summary>
/// <remarks>
/// <para>
/// Four values, and each of them silently invalidates every stored vector when it changes: an index built by
/// one model cannot be searched with a query embedded by another; one built at one width cannot be compared
/// with a query at another; one built from a different composition of a product's text is not comparable with
/// one built from this composition; and one built from an older <c>products.json</c> describes products that
/// may no longer exist. Recorded, each of those is a detectable fact instead of a wrong answer.
/// </para>
/// <para>
/// <b>It is written by the tool that ingests and read by the application that queries</b>, which is why it is
/// one record here rather than a table each side describes separately: the writer and the reader of a contract
/// are exactly the two parties that must agree on it.
/// </para>
/// </remarks>
/// <param name="ModelId">The model the vectors came from.</param>
/// <param name="Width">How many floats each vector holds.</param>
/// <param name="Composition">Which fields a product's text was made of, as a stable value. Compared, never parsed.</param>
/// <param name="CatalogueHash">The hash of the catalogue file as it was read.</param>
public sealed record EmbeddingRecipe(string ModelId, int Width, string Composition, string CatalogueHash);
