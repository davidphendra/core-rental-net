using CoreRentalNet.Modules.Workspace.Application.Queries.Views;

namespace CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.SetDeliveryAddress;

/// <summary>Records where the workspace behind a draft token should be delivered.</summary>
public interface ISetDeliveryAddress
{
    Task<WorkspaceView> HandleAsync(SetDeliveryAddress command, CancellationToken cancellationToken = default);
}
