using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Workspace;

/// <summary>Builds the view the UI renders from the draft, the slot table and the catalog.</summary>
public static class WorkspaceViewFactory
{
    public static WorkspaceView Build(Domain.Workspace workspace, IDefineProductPrices prices)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(prices);

        var quote = WorkspaceQuoter.Quote(workspace, prices);

        var slots = SlotRules.All
            .Select(rule =>
            {
                var assignment = workspace.AssignmentFor(rule.Slot);
                var line = quote.Lines.FirstOrDefault(candidate => candidate.Slot == rule.Slot);

                return new AssignableSlot(
                    rule.Slot,
                    rule.DisplayName,
                    rule.MaxQuantity,
                    assignment?.Sku,
                    line is null || !line.IsAvailable ? null : line.Name,
                    assignment?.Quantity ?? 0,
                    line?.UnitMonthlyPrice);
            })
            .ToArray();

        return new WorkspaceView(
            workspace.Id.Value,
            workspace.State,
            workspace.Version,
            slots,
            workspace.DeliveryAddress,
            workspace.TotalUnits,
            workspace.IsEmpty,
            quote);
    }
}
