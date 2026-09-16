using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Application.Queries.SearchCatalog;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// A catalogue that fails while it is being read, so the failure path can be observed end to end.
/// </summary>
/// <remarks>
/// The message is distinctive on purpose: the test asserts it does **not** reach the caller, and it
/// could only make that claim about a string nobody else would send.
/// </remarks>
internal sealed class ThrowingCatalogHandler : ISearchCatalogHandler
{
    public const string Message = "The catalogue failed while it was being read.";

    public IReadOnlyList<ProductView> Handle(SearchCatalogQuery query)
        => throw new InvalidOperationException(Message);
}
