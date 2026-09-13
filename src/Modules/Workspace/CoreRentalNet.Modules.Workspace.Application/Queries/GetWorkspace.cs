using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Workspace;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Queries;

public sealed record GetWorkspace(string DraftToken);

/// <summary>The workspace behind a draft token, priced and ready to render.</summary>
public interface IGetWorkspace
{
    Task<WorkspaceView> HandleAsync(GetWorkspace query, CancellationToken cancellationToken = default);
}

public sealed class GetWorkspaceHandler(IWorkspaceRepository repository, IDefineProductPrices prices) : IGetWorkspace
{
    public async Task<WorkspaceView> HandleAsync(GetWorkspace query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var workspace = await WorkspaceResolver.ResolveAsync(repository, query.DraftToken, cancellationToken).ConfigureAwait(false);

        return WorkspaceViewFactory.Build(workspace, prices);
    }
}
