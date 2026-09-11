using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Workspace;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Commands;

/// <summary>Sets how many of one product a slot holds. Zero removes that product.</summary>
public sealed record ChangeQuantity(string DraftToken, SlotId Slot, string Sku, int Quantity);

public sealed class ChangeQuantityHandler(IWorkspaceRepository repository, IDefineProductPrices prices)
{
    public async Task<WorkspaceView> HandleAsync(ChangeQuantity command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var workspace = await WorkspaceResolver.ResolveAsync(repository, command.DraftToken, cancellationToken).ConfigureAwait(false);

        workspace.ChangeQuantity(command.Slot, command.Sku, command.Quantity);

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return WorkspaceViewFactory.Build(workspace, prices);
    }
}
