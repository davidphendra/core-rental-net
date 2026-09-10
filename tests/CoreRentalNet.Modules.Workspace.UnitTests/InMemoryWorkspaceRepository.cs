using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.UnitTests;

internal sealed class InMemoryWorkspaceRepository : IWorkspaceRepository
{
    private readonly Dictionary<string, Domain.Workspace> workspacesByTokenHash = new(StringComparer.Ordinal);

    public int SaveCount { get; private set; }

    public Task<Domain.Workspace?> FindByTokenAsync(DraftToken token, CancellationToken cancellationToken = default)
        => Task.FromResult(workspacesByTokenHash.TryGetValue(token.Hash, out var workspace) ? workspace : null);

    public Task AddAsync(Domain.Workspace workspace, CancellationToken cancellationToken = default)
    {
        workspacesByTokenHash[workspace.DraftTokenHash] = workspace;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
