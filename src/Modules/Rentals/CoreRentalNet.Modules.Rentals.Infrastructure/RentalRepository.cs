using CoreRentalNet.Modules.Rentals.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreRentalNet.Modules.Rentals.Infrastructure;

public sealed class RentalRepository(RentalsContext context) : IRentalRepository
{
    public async Task<Domain.Rental?> FindByTokenAsync(AccessToken token, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        return await context.Rentals
            .FirstOrDefaultAsync(rental => rental.AccessTokenHash == token.Hash, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Domain.Rental?> FindByIdAsync(RentalId id, CancellationToken cancellationToken = default)
        => await context.Rentals.FirstOrDefaultAsync(rental => rental.Id == id, cancellationToken).ConfigureAwait(false);

    public async Task AddAsync(Domain.Rental rental, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rental);

        await context.Rentals.AddAsync(rental, cancellationToken).ConfigureAwait(false);
    }
}
