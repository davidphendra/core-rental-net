namespace CoreRentalNet.Modules.Workspace.Application.Commands.RemoveAssignment;

/// <summary>Empties a slot of the workspace behind a draft token.</summary>
/// <remarks>A command handler mutates state and returns nothing; the caller re-reads to render.</remarks>
public interface IRemoveAssignmentHandler
{
    Task HandleAsync(RemoveAssignmentCommand command, CancellationToken cancellationToken = default);
}
