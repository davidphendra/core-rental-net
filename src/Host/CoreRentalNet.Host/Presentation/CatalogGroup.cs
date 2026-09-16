using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Host.Presentation;

/// <summary>One subcategory's products under the label they are shown beneath.</summary>
public sealed record CatalogGroup(
    CatalogSubCategory? SubCategory,
    string Label,
    IReadOnlyList<ProductView> Products);
