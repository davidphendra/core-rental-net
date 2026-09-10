using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Workspace;

/// <summary>
/// What the workspace costs per month. Every amount is recomputed from the catalog on each
/// read; nothing here is stored (ADR-0006).
/// </summary>
public sealed record WorkspaceQuote(IReadOnlyList<QuoteLine> Lines, Money MonthlySubtotal)
{
    public bool IsEmpty => Lines.Count == 0;

    public bool HasUnavailableLines => Lines.Any(line => !line.IsAvailable);

    public IReadOnlyList<QuoteLine> UnavailableLines
        => Lines.Where(line => !line.IsAvailable).ToArray();

    public int TotalUnits => Lines.Sum(line => line.Quantity);
}
