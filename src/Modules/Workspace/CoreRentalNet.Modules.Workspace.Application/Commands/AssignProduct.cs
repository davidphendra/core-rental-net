using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Catalog;
using CoreRentalNet.Modules.Workspace.Application.Workspace;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Commands;

/// <summary>
/// Adds a product to the draft. The customer supplies a SKU, never a slot: the slot comes
/// from what the product is.
/// </summary>
public sealed record AssignProduct(string DraftToken, string Sku, int Quantity = 1);

/// <summary>Adds units of a product to a slot of the workspace behind a draft token.</summary>
public interface IAssignProduct
{
    Task<WorkspaceView> HandleAsync(AssignProduct command, CancellationToken cancellationToken = default);
}

public sealed class AssignProductHandler(IWorkspaceRepository repository, IDefineProductPrices prices) : IAssignProduct
{
    public async Task<WorkspaceView> HandleAsync(AssignProduct command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var price = prices.FindPrice(command.Sku)
            ?? throw new DomainRuleViolationException($"The catalog does not know the product '{command.Sku}'.");

        var slot = CatalogSlotMapping.SlotFor(price.Category, price.SubCategory);
        var workspace = await WorkspaceResolver.ResolveAsync(repository, command.DraftToken, cancellationToken).ConfigureAwait(false);

        workspace.Assign(slot, price.Sku, command.Quantity);

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return WorkspaceViewFactory.Build(workspace, prices);
    }
}
