using CoreRentalNet.Modules.Rentals.Application.Rentals;
using CoreRentalNet.Modules.Rentals.Domain.Persistence;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.Application.Scheduling;

/// <summary>
/// The only thing in this application that moves an order forward without a person asking.
/// </summary>
/// <remarks>
/// There is no administrator and no operator, so without this a rental would freeze at the moment
/// it was paid for and "month to month" would be a phrase on the home page rather than a product.
/// <para>
/// Each order is taken as far as the date allows in one pass, so a run that happens after a gap
/// catches up rather than losing the periods it missed. The billing work is delegated to
/// <see cref="IRenewalInvoiceIssuer"/>; this class owns the order's status transitions and the pass.
/// </para>
/// </remarks>
public sealed class RentalScheduler(
    IRentalRepository rentals,
    IRentalLifecycleService lifecycle,
    IDeliveryPolicyService deliveries,
    IRenewalInvoiceIssuer invoicer,
    IUnitOfWork unitOfWork) : IRunRentalSchedule
{
    /// <inheritdoc />
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
        Rental rental,
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
    private void ScheduleDelivery(Rental rental, List<RentalScheduleAction> actions)
    {
        if (rental.Status != RentalStatus.Paid)
        {
            return;
        }

        lifecycle.ScheduleDelivery(rental, deliveries.ScheduledFor(rental.PlacedOn));
        actions.Add(new RentalScheduleAction(rental.Number.Value, ScheduleActionKind.DeliveryScheduled));
    }

    /// <summary>Delivered: the months start running from the scheduled date.</summary>
    private void ActivateIfDue(Rental rental, DateOnly asOf, List<RentalScheduleAction> actions)
    {
        if (rental.Status != RentalStatus.DeliveryScheduled
            || rental.DeliveryScheduledFor is not { } scheduled
            || scheduled > asOf)
        {
            return;
        }

        lifecycle.Activate(rental, asOf);
        actions.Add(new RentalScheduleAction(rental.Number.Value, ScheduleActionKind.Activated));
    }

    /// <summary>Cancelled: the equipment goes back when the paid month is over.</summary>
    private void EndIfDue(Rental rental, DateOnly asOf, List<RentalScheduleAction> actions)
    {
        if (rental.Status != RentalStatus.CancellationRequested
            || rental.EndsOn is not { } endsOn
            || endsOn > asOf)
        {
            return;
        }

        lifecycle.End(rental, asOf);
        actions.Add(new RentalScheduleAction(rental.Number.Value, ScheduleActionKind.Ended));
    }

    private async Task InvoiceStartedPeriodsAsync(
        Rental rental,
        DateOnly asOf,
        List<RentalScheduleAction> actions,
        CancellationToken cancellationToken)
    {
        var issued = await invoicer.IssueStartedPeriodsAsync(rental, asOf, cancellationToken).ConfigureAwait(false);

        actions.AddRange(issued);
    }
}
