namespace CoreRentalNet.Modules.Workspace.Application.Commands.AssignProduct;

/// <summary>Adds units of a product to a slot of the workspace behind a draft token.</summary>
/// <remarks>
/// A command handler mutates state and returns nothing. The caller re-reads through
/// <c>IGetWorkspaceHandler</c> when it needs the canvas; the write path does not know the read shape.
/// </remarks>
public interface IAssignProductHandler
{
    Task HandleAsync(AssignProductCommand command, CancellationToken cancellationToken = default);
}
