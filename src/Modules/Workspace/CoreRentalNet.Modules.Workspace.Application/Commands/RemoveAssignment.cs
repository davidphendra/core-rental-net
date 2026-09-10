using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Workspace;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Commands;

/// <summary>Empty a slot. Removing an empty slot is a no-op, so a double click is harmless.</summary>
public sealed record RemoveAssignment(string DraftToken, SlotId Slot);

public sealed class RemoveAssignmentHandler(IWorkspaceRepository repository, IDefineProductPrices prices)
{
    public async Task<WorkspaceView> HandleAsync(RemoveAssignment command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var workspace = await WorkspaceResolver.ResolveAsync(repository, command.DraftToken, cancellationToken).ConfigureAwait(false);

        if (workspace.Remove(command.Slot))
        {
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return WorkspaceViewFactory.Build(workspace, prices);
    }
}
