namespace CoreRentalNet.Modules.Catalog.Application.Contracts;

/// <summary>
/// The catalogue's stored vectors cannot be searched right now.
/// </summary>
/// <remarks>
/// <para>
/// <b>Typed so a caller can tell an outage from a defect.</b> An embedding deployment that did not answer and
/// a vector file that is absent are the same fact to a caller - there is nothing to rank by - and neither is
/// something this application got wrong, so both arrive as this rather than as a failure to explain.
/// </para>
/// <para>
/// <b>Cancellation is deliberately not this.</b> A caller who stopped the request has not met an outage, and
/// wrapping it here would turn a deliberate action into a failure.
/// </para>
/// </remarks>
public sealed class ProductSimilarityUnavailableException : Exception
{
    /// <summary>The failure on its own.</summary>
    public ProductSimilarityUnavailableException(string message)
        : base(message)
    {
    }

    /// <summary>The failure, with what the deployment or the file said underneath it.</summary>
    public ProductSimilarityUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
