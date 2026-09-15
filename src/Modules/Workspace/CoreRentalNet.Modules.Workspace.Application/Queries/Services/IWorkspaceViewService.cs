using CoreRentalNet.Modules.Workspace.Application.Queries.Views;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Queries.Services;

/// <summary>Builds the view the UI renders from a draft, the slot table and the catalog.</summary>
public interface IWorkspaceViewService
{
    WorkspaceView Build(Domain.Workspace workspace);
}
