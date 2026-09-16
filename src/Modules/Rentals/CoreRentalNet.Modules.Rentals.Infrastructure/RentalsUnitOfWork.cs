
using CoreRentalNet.Modules.Rentals.Domain.Persistence;

namespace CoreRentalNet.Modules.Rentals.Infrastructure;

/// <summary>Saves every change one scheduler pass or checkout made, in one transaction.</summary>
public sealed class RentalsUnitOfWork(RentalsContext context) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}
