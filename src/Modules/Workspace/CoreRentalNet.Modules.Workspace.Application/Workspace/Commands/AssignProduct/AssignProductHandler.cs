using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Rules;
using CoreRentalNet.Modules.Workspace.Application.Queries.Services;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Services;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Support;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.AssignProduct;

/// <summary>Puts a catalogue product into the slot the customer picked.</summary>
public sealed class AssignProductHandler(
    IWorkspaceRepository repository,
    IOpaqueTokenService tokens,
    IProductCatalog catalog,
    IWorkspaceService workspaceService) : IAssignProductHandler
{
    /// <inheritdoc />
    public async Task HandleAsync(AssignProductCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var price = catalog.Find(command.Sku)
            ?? throw new DomainRuleViolationException($"The catalog does not know the product '{command.Sku}'.");

        var slot = CatalogSlotMapping.SlotFor(price.Category, price.SubCategory);
        var workspace = await WorkspaceResolver.ResolveAsync(repository, tokens, command.DraftToken, cancellationToken).ConfigureAwait(false);

        workspaceService.Assign(workspace, slot, price.Sku, command.Quantity);

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
