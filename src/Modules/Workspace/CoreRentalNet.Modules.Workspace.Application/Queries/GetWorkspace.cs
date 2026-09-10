using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Workspace;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Queries;

public sealed record GetWorkspace(string DraftToken);

public sealed class GetWorkspaceHandler(IWorkspaceRepository repository, IDefineProductPrices prices)
{
    public async Task<WorkspaceView> HandleAsync(GetWorkspace query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var workspace = await WorkspaceResolver.ResolveAsync(repository, query.DraftToken, cancellationToken).ConfigureAwait(false);

        return WorkspaceViewFactory.Build(workspace, prices);
    }
}
