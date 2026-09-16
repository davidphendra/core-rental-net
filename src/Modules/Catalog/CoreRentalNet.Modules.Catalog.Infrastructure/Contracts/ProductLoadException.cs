namespace CoreRentalNet.Modules.Catalog.Infrastructure.Contracts;

/// <summary>
/// Raised when the catalog file cannot be read or does not describe a valid catalog.
/// Deliberately loud: a broken catalog must stop the application at start-up rather than
/// produce a half-populated shop.
/// </summary>
public sealed class ProductLoadException : Exception
{
    public ProductLoadException(string message)
        : base(message)
    {
    }

    public ProductLoadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
