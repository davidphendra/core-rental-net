using CoreRentalNet.Modules.Rentals.Domain;

namespace CoreRentalNet.Modules.Rentals.Application.Scheduling;

/// <summary>
/// The only thing in this application that moves an order forward without a person asking.
/// </summary>
/// <remarks>
/// There is no administrator and no operator, so without this a rental would freeze at the moment
/// it was paid for and "month to month" would be a phrase on the home page rather than a product.
/// <para>
/// Each order is taken as far as the date allows in one pass, so a run that happens after a gap
/// catches up rather than losing the periods it missed. Renewals are worked out from the invoices
/// that already exist and the period the date falls in, which is what makes a second run a no-op
/// and a duplicate impossible.
/// </para>
/// </remarks>
public sealed class RentalScheduler(
    IRentalRepository rentals,
    IInvoiceRepository invoices,
    INumberSequence numbers,
    IUnitOfWork unitOfWork,
    RentalsSettings settings) : IRunRentalSchedule
{
    public async Task<SchedulingOutcome> RunOnceAsync(DateOnly asOf, CancellationToken cancellationToken = default)
    {
        var actions = new List<RentalScheduleAction>();
        var failures = new List<string>();

        var schedulable = await rentals.ListSchedulableAsync(cancellationToken).ConfigureAwait(false);

        foreach (var rental in schedulable)
        {
            try
            {
                await AdvanceAsync(rental, asOf, actions, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failures.Add($"{rental.Number}: {exception.Message}");
            }
        }

        // Saved unconditionally. An earlier version only saved when the order itself had changed,
        // which silently dropped renewals: issuing an invoice does not touch the order, so a pass
        // whose only work was billing wrote nothing at all. Saving with nothing to save costs
        // nothing, and the alternative is a rule that is easy to get wrong.
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new SchedulingOutcome(asOf, actions, failures);
    }

    private async Task AdvanceAsync(
        Domain.Rental rental,
        DateOnly asOf,
        List<RentalScheduleAction> actions,
        CancellationToken cancellationToken)
    {
        ScheduleDelivery(rental, actions);
        ActivateIfDue(rental, asOf, actions);

        // Live: every period that has begun is billed once, and settled immediately because payment
        // cannot fail here.
        if (rental.Status == RentalStatus.Active)
        {
            await InvoiceStartedPeriodsAsync(rental, asOf, actions, cancellationToken).ConfigureAwait(false);
        }

        EndIfDue(rental, asOf, actions);
    }

    /// <summary>Paid: the setup is on its way.</summary>
    private static void ScheduleDelivery(Domain.Rental rental, List<RentalScheduleAction> actions)
    {
        if (rental.Status != RentalStatus.Paid)
        {
            return;
        }

        rental.ScheduleDelivery(DeliveryPolicy.ScheduledFor(rental.PlacedOn));
        actions.Add(new RentalScheduleAction(rental.Number.Value, ScheduleActionKind.DeliveryScheduled));
    }

    /// <summary>Delivered: the months start running from the scheduled date.</summary>
    private static void ActivateIfDue(Domain.Rental rental, DateOnly asOf, List<RentalScheduleAction> actions)
    {
        if (rental.Status != RentalStatus.DeliveryScheduled
            || rental.DeliveryScheduledFor is not { } scheduled
            || scheduled > asOf)
        {
            return;
        }

        rental.Activate(asOf);
        actions.Add(new RentalScheduleAction(rental.Number.Value, ScheduleActionKind.Activated));
    }

    /// <summary>Cancelled: the equipment goes back when the paid month is over.</summary>
    private static void EndIfDue(Domain.Rental rental, DateOnly asOf, List<RentalScheduleAction> actions)
    {
        if (rental.Status != RentalStatus.CancellationRequested
            || rental.EndsOn is not { } endsOn
            || endsOn > asOf)
        {
            return;
        }

        rental.End(asOf);
        actions.Add(new RentalScheduleAction(rental.Number.Value, ScheduleActionKind.Ended));
    }

    private async Task InvoiceStartedPeriodsAsync(
        Domain.Rental rental,
        DateOnly asOf,
        List<RentalScheduleAction> actions,
        CancellationToken cancellationToken)
    {
        var issued = await invoices.ListForRentalAsync(rental.Id, cancellationToken).ConfigureAwait(false);

        var lastInvoiced = issued.Count == 0 ? -1 : issued.Max(invoice => invoice.PeriodIndex);
        var currentPeriod = rental.PeriodContaining(asOf).Index;

        for (var index = lastInvoiced + 1; index <= currentPeriod; index++)
        {
            var period = RentalPeriod.For(rental.AnchorDate, index);

            // A period that begins after the rental ends is never billed.
            if (rental.EndsOn is { } endsOn && period.Start >= endsOn)
            {
                break;
            }

            var number = InvoiceNumber.Of(
                period.Start.Year,
                await numbers.ReserveNextAsync(SequenceKind.Invoice, period.Start.Year, cancellationToken).ConfigureAwait(false));

            var invoice = Invoice.IssueFor(InvoiceId.New(), number, rental, index, settings.TaxRate, asOf);
            invoice.Settle(asOf);

            await invoices.AddAsync(invoice, cancellationToken).ConfigureAwait(false);
            actions.Add(new RentalScheduleAction(rental.Number.Value, ScheduleActionKind.InvoiceIssued, number.Value));
        }
    }
}
