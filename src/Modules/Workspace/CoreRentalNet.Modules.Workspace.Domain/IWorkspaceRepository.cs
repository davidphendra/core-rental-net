namespace CoreRentalNet.Modules.Workspace.Domain;

/// <summary>The draft store: declared in Domain, implemented by Infrastructure.</summary>
public interface IWorkspaceRepository
{
    Task<Workspace?> FindByTokenAsync(DraftToken token, CancellationToken cancellationToken = default);

    Task AddAsync(Workspace workspace, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
