namespace CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.SetDeliveryAddress;

/// <summary>Records where the workspace behind a draft token should be delivered.</summary>
/// <remarks>A command handler mutates state and returns nothing; the caller re-reads to render.</remarks>
public interface ISetDeliveryAddressHandler
{
    Task HandleAsync(SetDeliveryAddressCommand command, CancellationToken cancellationToken = default);
}
