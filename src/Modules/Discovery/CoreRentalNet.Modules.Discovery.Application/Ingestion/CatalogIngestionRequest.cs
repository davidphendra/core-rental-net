using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Discovery.Application.Ingestion;

/// <summary>
/// Everything one ingestion run is given: the catalogue, what it was, and what will embed it.
/// </summary>
/// <remarks>
/// <para>
/// The width and the model id are <b>passed in rather than read from the vectors</b>, so that a deployment
/// answering with the wrong width is a refusal instead of a fact. Inferring the width from the answer would
/// mean an index that silently records whatever it was handed, which is the one number the query path has to
/// be able to trust.
/// </para>
/// <para>
/// The hash is of the catalogue file as it was read. It is not the suggestion run's payload hash: that one
/// describes the projection a model was shown, and this one describes the file an index was built from.
/// </para>
/// </remarks>
public sealed record CatalogIngestionRequest(
    IReadOnlyList<ProductView> Catalogue,
    string CatalogueHash,
    string ModelId,
    int Width);
