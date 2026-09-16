namespace CoreRentalNet.Modules.Rentals.Domain.Persistence;

/// <summary>
/// Saves everything a use case changed, in one transaction. An order, its first invoice and the
/// cancelled sequence reservations either all land or none do.
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
