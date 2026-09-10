using CoreRentalNet.Modules.Rentals.Domain;

namespace CoreRentalNet.Modules.Rentals.Infrastructure;

public sealed class RentalsUnitOfWork(RentalsContext context) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}
