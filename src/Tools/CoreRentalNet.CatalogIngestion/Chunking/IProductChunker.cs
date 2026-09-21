namespace CoreRentalNet.CatalogIngestion.Chunking;

/// <summary>Splits one product's text into pieces and embeds each piece.</summary>
/// <remarks>
/// <para>
/// <b>It embeds as well as splits, and that is not a leak.</b> Deciding where a boundary falls means measuring
/// the distance between neighbouring sentences, so the splitter has to embed them; a contract that returned
/// unembedded pieces would make the same text be embedded twice — once to choose the boundaries and once to
/// store the result.
/// </para>
/// <para>
/// <b>The pieces' text is not returned</b>, because nothing keeps it: the table stores the vector beside the
/// product's <c>name</c> and <c>description</c>, not the passages they were cut from.
/// </para>
/// </remarks>
internal interface IProductChunker
{
    /// <summary>The vectors of one product's chunks, in the order the chunks were assembled.</summary>
    Task<IReadOnlyList<float[]>> ChunkAsync(string text, CancellationToken cancellationToken);
}
