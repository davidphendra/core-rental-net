using Microsoft.EntityFrameworkCore;
using CoreRentalNet.Modules.Rentals.Domain.Invoices;
using CoreRentalNet.Modules.Rentals.Domain.Persistence;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.Infrastructure;

/// <summary>The EF Core adapter behind <see cref="Domain.Persistence.IInvoiceRepository"/>.</summary>
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
