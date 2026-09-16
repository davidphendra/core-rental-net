using CoreRentalNet.Modules.Rentals.Application.Views;

namespace CoreRentalNet.Modules.Rentals.Application.Queries.GetInvoicesByToken;

/// <summary>Every invoice raised for the order behind a token, oldest first.</summary>
public interface IGetInvoicesByTokenHandler
{
    Task<IReadOnlyList<InvoiceView>> HandleAsync(
        GetInvoicesByTokenQuery query,
        CancellationToken cancellationToken = default);
}
