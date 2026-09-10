using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Workspace;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Commands;

public sealed record SetDeliveryAddress(string DraftToken, string? Address);

public sealed class SetDeliveryAddressHandler(IWorkspaceRepository repository, IDefineProductPrices prices)
{
    public async Task<WorkspaceView> HandleAsync(SetDeliveryAddress command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var workspace = await WorkspaceResolver.ResolveAsync(repository, command.DraftToken, cancellationToken).ConfigureAwait(false);

        workspace.SetDeliveryAddress(command.Address);

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return WorkspaceViewFactory.Build(workspace, prices);
    }
}
