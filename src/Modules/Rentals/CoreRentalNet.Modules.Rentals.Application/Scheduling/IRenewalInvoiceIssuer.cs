using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.Application.Scheduling;

/// <summary>
/// Bills every period of one live order that has begun and has not been invoiced yet.
/// </summary>
public interface IRenewalInvoiceIssuer
{
    /// <summary>
    /// Issues and settles one invoice per started, unbilled period, and reports what it did.
    /// </summary>
    Task<IReadOnlyList<RentalScheduleAction>> IssueStartedPeriodsAsync(
        Rental rental,
        DateOnly asOf,
        CancellationToken cancellationToken = default);
}
