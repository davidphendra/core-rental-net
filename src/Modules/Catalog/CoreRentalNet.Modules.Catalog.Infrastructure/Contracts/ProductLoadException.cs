namespace CoreRentalNet.Modules.Catalog.Infrastructure.Contracts;

/// <summary>
/// Raised when the catalogService file cannot be read or does not describe a valid catalogService.
/// Deliberately loud: a broken catalogService must stop the application at start-up rather than
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
