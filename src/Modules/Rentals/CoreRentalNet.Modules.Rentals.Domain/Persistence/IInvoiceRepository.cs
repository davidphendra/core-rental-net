using CoreRentalNet.Modules.Rentals.Domain.Invoices;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.Domain.Persistence;

/// <summary>The invoice store: declared in Domain, implemented by Infrastructure.</summary>
public interface IInvoiceRepository
{
    Task<IReadOnlyList<Invoice>> ListForRentalAsync(RentalId rentalId, CancellationToken cancellationToken = default);

    Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default);
}
