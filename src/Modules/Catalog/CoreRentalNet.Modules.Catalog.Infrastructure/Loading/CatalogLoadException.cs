namespace CoreRentalNet.Modules.Catalog.Infrastructure.Loading;

/// <summary>
/// Raised when the catalog file cannot be read or does not describe a valid catalog.
/// Deliberately loud: a broken catalog must stop the application at start-up rather than
/// produce a half-populated shop.
/// </summary>
public sealed class CatalogLoadException : Exception
{
    public CatalogLoadException()
    {
    }

    public CatalogLoadException(string message)
        : base(message)
    {
    }

    public CatalogLoadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
