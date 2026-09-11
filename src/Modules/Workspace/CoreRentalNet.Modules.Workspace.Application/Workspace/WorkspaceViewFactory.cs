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

        // One item per product the slot holds, priced from the quote rather than from the assignment:
        // a product that has left the catalog has no price, and saying so is the quote's job.
        var slots = SlotRules.All
            .Select(rule => new AssignableSlot(
                rule.Slot,
                rule.DisplayName,
                rule.MaxQuantity,
                [.. workspace.AssignmentsFor(rule.Slot).Select(assignment => ItemFor(assignment, quote))]))
            .ToArray();

        return new WorkspaceView(
            workspace.Id.Value,
            workspace.State,
            workspace.Version,
            slots,
            workspace.DeliveryAddress,
            workspace.TotalUnits,
            workspace.IsEmpty,
            quote,
            [.. SlotRules.Mandatory
                .Where(rule => workspace.AssignmentsFor(rule.Slot).Count == 0)
                .Select(rule => rule.DisplayName)]);
    }

    private static AssignableItem ItemFor(SlotAssignment assignment, WorkspaceQuote quote)
    {
        var line = quote.Lines.FirstOrDefault(candidate => candidate.Sku == assignment.Sku);

        return line is null || !line.IsAvailable
            ? new AssignableItem(assignment.Sku, assignment.Sku, assignment.Quantity, null, null, false)
            : new AssignableItem(
                assignment.Sku,
                line.Name,
                assignment.Quantity,
                line.UnitMonthlyPrice,
                line.ImagePath,
                line.ImageAvailable);
    }
}
