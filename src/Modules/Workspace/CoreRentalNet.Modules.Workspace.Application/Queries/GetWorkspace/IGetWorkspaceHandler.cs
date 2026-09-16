using CoreRentalNet.Modules.Workspace.Application.Queries.Views;

namespace CoreRentalNet.Modules.Workspace.Application.Queries.GetWorkspace;

/// <summary>The workspace behind a draft token, priced and ready to render.</summary>
public interface IGetWorkspaceHandler
{
    Task<WorkspaceView> HandleAsync(GetWorkspaceQuery query, CancellationToken cancellationToken = default);
}
