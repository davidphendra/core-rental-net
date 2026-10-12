using CoreRentalNet.CatalogPopulate.Compact;

namespace CoreRentalNet.CatalogPopulate.Metadata;

/// <summary>One product to generate metadata for, with the bounded review sample the model may read.</summary>
internal sealed record MetadataGenerationRequest(CompactProduct Product, IReadOnlyList<CompactReview> Sample);
