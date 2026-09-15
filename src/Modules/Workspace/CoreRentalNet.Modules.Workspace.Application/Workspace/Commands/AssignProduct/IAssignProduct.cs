using CoreRentalNet.Modules.Workspace.Application.Queries.Views;

namespace CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.AssignProduct;

/// <summary>Adds units of a product to a slot of the workspace behind a draft token.</summary>
public interface IAssignProduct
{
    Task<WorkspaceView> HandleAsync(AssignProduct command, CancellationToken cancellationToken = default);
}
