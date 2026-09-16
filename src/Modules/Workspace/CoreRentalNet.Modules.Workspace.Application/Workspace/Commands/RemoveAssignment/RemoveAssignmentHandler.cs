using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.Modules.Workspace.Application.Queries.Services;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Services;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Support;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.RemoveAssignment;

/// <summary>Takes an assigned product out of the draft.</summary>
public sealed class RemoveAssignmentHandler(
    IWorkspaceRepository repository,
    IOpaqueTokenService tokens,
    IWorkspaceService workspaceService) : IRemoveAssignmentHandler
{
    /// <inheritdoc />
    public async Task HandleAsync(RemoveAssignmentCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var workspace = await WorkspaceResolver.ResolveAsync(repository, tokens, command.DraftToken, cancellationToken).ConfigureAwait(false);

        if (workspaceService.Remove(workspace, command.Slot))
        {
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
