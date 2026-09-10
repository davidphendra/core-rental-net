namespace CoreRentalNet.Modules.Workspace.Domain;

public interface IWorkspaceRepository
{
    Task<Workspace?> FindByTokenAsync(DraftToken token, CancellationToken cancellationToken = default);

    Task AddAsync(Workspace workspace, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
