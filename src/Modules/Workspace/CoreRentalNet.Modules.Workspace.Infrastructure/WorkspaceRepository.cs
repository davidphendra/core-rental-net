using CoreRentalNet.Modules.Workspace.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreRentalNet.Modules.Workspace.Infrastructure;

/// <summary>The EF Core adapter behind <see cref="Domain.IWorkspaceRepository"/>.</summary>
public sealed class WorkspaceRepository(WorkspaceContext context) : IWorkspaceRepository
{
    public async Task<Domain.Workspace?> FindByTokenAsync(
        DraftToken token,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        return await context.Drafts
            .FirstOrDefaultAsync(draft => draft.DraftTokenHash == token.Hash, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Domain.Workspace workspace, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        await context.Drafts.AddAsync(workspace, cancellationToken).ConfigureAwait(false);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}
