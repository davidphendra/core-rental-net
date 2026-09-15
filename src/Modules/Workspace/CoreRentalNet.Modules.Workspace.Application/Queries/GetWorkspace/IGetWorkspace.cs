using CoreRentalNet.Modules.Workspace.Application.Queries.Views;

namespace CoreRentalNet.Modules.Workspace.Application.Queries.GetWorkspace;

/// <summary>The workspace behind a draft token, priced and ready to render.</summary>
public interface IGetWorkspace
{
    Task<WorkspaceView> HandleAsync(GetWorkspace query, CancellationToken cancellationToken = default);
}
