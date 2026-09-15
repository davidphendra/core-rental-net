namespace CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.SetDeliveryAddress;

/// <summary>The address the order will be delivered to.</summary>
public sealed record SetDeliveryAddress(string DraftToken, string? Address);
