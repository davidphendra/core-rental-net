
using CoreRentalNet.Modules.Rentals.Domain.Persistence;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.UnitTests;

internal sealed class InMemoryRentalRepository : IRentalRepository
{
    private readonly List<Rental> rentals = [];

    public IReadOnlyList<Rental> All => rentals;

    public Task<Rental?> FindByTokenAsync(AccessToken token, CancellationToken cancellationToken = default)
        => Task.FromResult(rentals.FirstOrDefault(rental => rental.AccessTokenHash == token.Hash));

    public Task<Rental?> FindByIdAsync(RentalId id, CancellationToken cancellationToken = default)
        => Task.FromResult(rentals.FirstOrDefault(rental => rental.Id == id));

    public void Seed(Rental rental) => rentals.Add(rental);

    public Task<Rental?> FindByWorkspaceIdAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => Task.FromResult(rentals.FirstOrDefault(rental => rental.WorkspaceId == workspaceId));

    public Task<IReadOnlyList<Rental>> ListSchedulableAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Rental>>(rentals
            .Where(rental => rental.Status is RentalStatus.Paid or RentalStatus.DeliveryScheduled
                or RentalStatus.Active or RentalStatus.CancellationRequested)
            .ToArray());

    public Task AddAsync(Rental rental, CancellationToken cancellationToken = default)
    {
        rentals.Add(rental);
        return Task.CompletedTask;
    }
}
