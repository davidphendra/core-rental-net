using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Workspace;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Commands;

/// <summary>
/// Ensures a draft exists for a token.
/// </summary>
/// <remarks>
/// Creating the draft is a write, so it lives in a command rather than inside the read query:
/// the query stays a query. Running it twice for the same token is harmless and returns the
/// same draft (matrix DR-07).
/// </remarks>
public sealed record StartDraft(string DraftToken);

public sealed class StartDraftHandler(IWorkspaceRepository repository, IDefineProductPrices prices)
{
    public async Task<WorkspaceView> HandleAsync(StartDraft command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var token = DraftToken.FromRawToken(command.DraftToken);
        var workspace = await repository.FindByTokenAsync(token, cancellationToken).ConfigureAwait(false);

        if (workspace is null)
        {
            workspace = Domain.Workspace.CreateNew(WorkspaceId.New(), token.Hash);
            await repository.AddAsync(workspace, cancellationToken).ConfigureAwait(false);
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return WorkspaceViewFactory.Build(workspace, prices);
    }
}
