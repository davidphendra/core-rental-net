namespace CoreRentalNet.CatalogPopulate.Metadata;

/// <summary>Produces the semantic and normalised metadata for one product.</summary>
internal interface IProductMetadataGenerator
{
    /// <summary>The generated metadata for the request's product.</summary>
    Task<GeneratedProductMetadata> GenerateAsync(MetadataGenerationRequest request, CancellationToken cancellationToken);
}
