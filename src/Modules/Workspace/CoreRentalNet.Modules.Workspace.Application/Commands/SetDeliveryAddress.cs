using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Workspace;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Commands;

public sealed record SetDeliveryAddress(string DraftToken, string? Address);

/// <summary>Records where the workspace behind a draft token should be delivered.</summary>
public interface ISetDeliveryAddress
{
    Task<WorkspaceView> HandleAsync(SetDeliveryAddress command, CancellationToken cancellationToken = default);
}

public sealed class SetDeliveryAddressHandler(IWorkspaceRepository repository, IDefineProductPrices prices) : ISetDeliveryAddress
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
