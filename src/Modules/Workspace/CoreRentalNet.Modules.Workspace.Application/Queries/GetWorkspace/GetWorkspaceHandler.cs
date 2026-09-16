using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.Modules.Workspace.Application.Queries.Services;
using CoreRentalNet.Modules.Workspace.Application.Queries.Views;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Support;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Queries.GetWorkspace;

/// <summary>Answers <see cref="IGetWorkspaceHandler"/> with the draft as the builder renders it.</summary>
public sealed class GetWorkspaceHandler(
    IWorkspaceRepository repository,
    IOpaqueTokenService tokens,
    IWorkspaceViewService views) : IGetWorkspaceHandler
{
    public async Task<WorkspaceView> HandleAsync(GetWorkspaceQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var workspace = await WorkspaceResolver.ResolveAsync(repository, tokens, query.DraftToken, cancellationToken).ConfigureAwait(false);

        return views.Build(workspace);
    }
}
