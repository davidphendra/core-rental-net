using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.Modules.Workspace.Application.Services;
using CoreRentalNet.Modules.Workspace.Application.Support;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Commands.ChangeQuantity;

/// <summary>Changes how many of one assigned product the draft holds.</summary>
public sealed class ChangeQuantityHandler(
    IWorkspaceRepository repository,
    IOpaqueTokenService tokens,
    IWorkspaceService workspaceService) : IChangeQuantityHandler
{
    /// <inheritdoc />
    public async Task HandleAsync(ChangeQuantityCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var workspace = await WorkspaceResolver.ResolveAsync(repository, tokens, command.DraftToken, cancellationToken).ConfigureAwait(false);

        workspaceService.ChangeQuantity(workspace, command.Slot, command.Sku, command.Quantity);

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
