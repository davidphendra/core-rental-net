using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Host.Tests;

/// <summary>The MCP factory with an unusable vector index, so a tool's failure answer can be asserted.</summary>
/// <remarks>
/// The reason carries a path, which is what the real adapter's refusal does: the test is that the tool answers
/// with the model-safe message and lets the operator's reason reach only the log.
/// </remarks>
public sealed class CatalogMcpUnavailableFactory : CatalogMcpFactory
{
    /// <summary>The operator's reason, which must never appear in a tool answer.</summary>
    public const string Reason =
        "The vector file '/tmp/secret/product_embedding.db' does not exist, so the catalogue cannot be searched.";

    protected override IProductSimilarityService Similarity(IProductCatalogService catalogue)
        => new UnavailableProductSimilarityService(Reason);
}
