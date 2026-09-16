using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.Modules.Workspace.Application.Queries.Services;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Services;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Support;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.SetDeliveryAddress;

/// <summary>Records the address, or clears it when the customer empties the field.</summary>
public sealed class SetDeliveryAddressHandler(
    IWorkspaceRepository repository,
    IOpaqueTokenService tokens,
    IWorkspaceService workspaceService) : ISetDeliveryAddressHandler
{
    /// <inheritdoc />
    public async Task HandleAsync(SetDeliveryAddressCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var workspace = await WorkspaceResolver.ResolveAsync(repository, tokens, command.DraftToken, cancellationToken).ConfigureAwait(false);

        workspaceService.SetDeliveryAddress(workspace, command.Address);

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
