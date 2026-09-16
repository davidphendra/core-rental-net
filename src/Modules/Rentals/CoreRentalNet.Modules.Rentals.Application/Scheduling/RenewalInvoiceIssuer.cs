using CoreRentalNet.Modules.Rentals.Application.Invoicing;
using CoreRentalNet.Modules.Rentals.Application.Rentals;
using CoreRentalNet.Modules.Rentals.Domain.Invoices;
using CoreRentalNet.Modules.Rentals.Domain.Numbering;
using CoreRentalNet.Modules.Rentals.Domain.Persistence;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.Application.Scheduling;

/// <summary>
/// The billing half of the schedule: the periods that have begun since the last pass.
/// </summary>
/// <remarks>
/// Split out of <see cref="RentalScheduler"/> so that the pass that moves an order's status and the
/// pass that bills a period are two readable things. Renewals are worked out from the invoices that
/// already exist and the period the date falls in, which is what makes a second run a no-op and a
/// duplicate invoice impossible.
/// </remarks>
public sealed class RenewalInvoiceIssuer(
    IInvoiceRepository invoices,
    IRentalLifecycleService lifecycle,
    IInvoiceService invoicing,
    IRenewalPolicyService renewals,
    INumberSequence numbers,
    RentalsSettings settings) : IRenewalInvoiceIssuer
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<RentalScheduleAction>> IssueStartedPeriodsAsync(
        Rental rental,
        DateOnly asOf,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rental);

        var issued = await invoices.ListForRentalAsync(rental.Id, cancellationToken).ConfigureAwait(false);

        var lastInvoiced = issued.Count == 0 ? -1 : issued.Max(invoice => invoice.PeriodIndex);
        var currentPeriod = lifecycle.PeriodContaining(rental, asOf).Index;
        var actions = new List<RentalScheduleAction>();

        for (var index = lastInvoiced + 1; index <= currentPeriod; index++)
        {
            var period = renewals.For(rental.AnchorDate, index);

            // A period that begins after the rental ends is never billed.
            if (rental.EndsOn is { } endsOn && period.Start >= endsOn)
            {
                break;
            }

            var number = InvoiceNumber.Of(
                period.Start.Year,
                await numbers.ReserveNextAsync(SequenceKind.Invoice, period.Start.Year, cancellationToken).ConfigureAwait(false));

            var invoice = invoicing.IssueFor(InvoiceId.New(), number, rental, index, settings.TaxRate, asOf);
            invoicing.Settle(invoice, asOf);

            await invoices.AddAsync(invoice, cancellationToken).ConfigureAwait(false);
            actions.Add(new RentalScheduleAction(rental.Number.Value, ScheduleActionKind.InvoiceIssued, number.Value));
        }

        return actions;
    }
}
