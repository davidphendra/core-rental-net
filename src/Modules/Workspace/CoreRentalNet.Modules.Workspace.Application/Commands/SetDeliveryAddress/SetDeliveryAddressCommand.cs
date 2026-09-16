namespace CoreRentalNet.Modules.Workspace.Application.Commands.SetDeliveryAddress;

/// <summary>The address the order will be delivered to.</summary>
public sealed record SetDeliveryAddressCommand(string DraftToken, string? Address);
