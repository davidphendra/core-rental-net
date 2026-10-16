namespace CoreRentalNet.Modules.Catalog.Application.Contracts;

/// <summary>One bounded page of a scoped catalogue query, and how many rows the scope holds in total.</summary>
public sealed record CatalogPage(IReadOnlyList<ProductDto> Items, int Total);
