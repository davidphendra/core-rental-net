namespace CoreRentalNet.Modules.Rentals.Domain;

public interface IRentalRepository
{
    Task<Rental?> FindByTokenAsync(AccessToken token, CancellationToken cancellationToken = default);

    Task<Rental?> FindByIdAsync(RentalId id, CancellationToken cancellationToken = default);

    Task AddAsync(Rental rental, CancellationToken cancellationToken = default);
}
