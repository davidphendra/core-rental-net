namespace CoreRentalNet.Modules.Workspace.Application.Contracts.Composition;

/// <summary>One line of a frozen draft: what to deliver, and how many.</summary>
public sealed record WorkspaceCompositionLine(string Sku, int Quantity);
