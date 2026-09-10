namespace CoreRentalNet.Modules.Rentals.Domain;

public interface IInvoiceRepository
{
    Task<IReadOnlyList<Invoice>> ListForRentalAsync(RentalId rentalId, CancellationToken cancellationToken = default);

    Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default);
}
