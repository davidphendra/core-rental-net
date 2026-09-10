namespace CoreRentalNet.Modules.Rentals.Domain;

public interface IRentalRepository
{
    Task<Rental?> FindByTokenAsync(AccessToken token, CancellationToken cancellationToken = default);

    Task<Rental?> FindByIdAsync(RentalId id, CancellationToken cancellationToken = default);

    /// <summary>The order a draft became, if it became one.</summary>
    Task<Rental?> FindByWorkspaceIdAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    Task AddAsync(Rental rental, CancellationToken cancellationToken = default);
}
