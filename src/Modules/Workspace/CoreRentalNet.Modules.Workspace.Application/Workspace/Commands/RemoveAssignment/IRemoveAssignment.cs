using CoreRentalNet.Modules.Workspace.Application.Queries.Views;

namespace CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.RemoveAssignment;

/// <summary>Empties a slot of the workspace behind a draft token.</summary>
public interface IRemoveAssignment
{
    Task<WorkspaceView> HandleAsync(RemoveAssignment command, CancellationToken cancellationToken = default);
}
