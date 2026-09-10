using CoreRentalNet.Modules.Rentals.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreRentalNet.Modules.Rentals.Infrastructure;

public sealed class InvoiceRepository(RentalsContext context) : IInvoiceRepository
{
    public async Task<IReadOnlyList<Invoice>> ListForRentalAsync(RentalId rentalId, CancellationToken cancellationToken = default)
        => await context.Invoices
            .Where(invoice => invoice.RentalId == rentalId)
            .OrderBy(invoice => invoice.PeriodIndex)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        await context.Invoices.AddAsync(invoice, cancellationToken).ConfigureAwait(false);
    }
}
