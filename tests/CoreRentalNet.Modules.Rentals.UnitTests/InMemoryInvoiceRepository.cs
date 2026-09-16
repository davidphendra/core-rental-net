
using CoreRentalNet.Modules.Rentals.Domain.Invoices;
using CoreRentalNet.Modules.Rentals.Domain.Persistence;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.UnitTests;

internal sealed class InMemoryInvoiceRepository : IInvoiceRepository
{
    private readonly List<Invoice> invoices = [];

    public IReadOnlyList<Invoice> All => invoices;

    public Task<IReadOnlyList<Invoice>> ListForRentalAsync(RentalId rentalId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Invoice>>(invoices.Where(invoice => invoice.RentalId == rentalId).ToArray());

    public Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        invoices.Add(invoice);
        return Task.CompletedTask;
    }
}
