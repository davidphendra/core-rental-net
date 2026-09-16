using Microsoft.EntityFrameworkCore;
using CoreRentalNet.Modules.Rentals.Domain.Persistence;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.Infrastructure;

/// <summary>The EF Core adapter behind <see cref="Domain.Persistence.IRentalRepository"/>.</summary>
public sealed class RentalRepository(RentalsContext context) : IRentalRepository
{
    public async Task<Rental?> FindByTokenAsync(AccessToken token, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        return await context.Rentals
            .FirstOrDefaultAsync(rental => rental.AccessTokenHash == token.Hash, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Rental?> FindByIdAsync(RentalId id, CancellationToken cancellationToken = default)
        => await context.Rentals.FirstOrDefaultAsync(rental => rental.Id == id, cancellationToken).ConfigureAwait(false);

    public async Task<Rental?> FindByWorkspaceIdAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => await context.Rentals
            .FirstOrDefaultAsync(rental => rental.WorkspaceId == workspaceId, cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Rental>> ListSchedulableAsync(CancellationToken cancellationToken = default)
        => await context.Rentals
            .Where(rental => rental.Status == RentalStatus.Paid
                             || rental.Status == RentalStatus.DeliveryScheduled
                             || rental.Status == RentalStatus.Active
                             || rental.Status == RentalStatus.CancellationRequested)
            .OrderBy(rental => rental.Number)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(Rental rental, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rental);

        await context.Rentals.AddAsync(rental, cancellationToken).ConfigureAwait(false);
    }
}
