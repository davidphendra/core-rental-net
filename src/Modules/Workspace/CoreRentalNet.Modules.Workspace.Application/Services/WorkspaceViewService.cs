using CoreRentalNet.Modules.Workspace.Application.Queries.Views;
using CoreRentalNet.Modules.Workspace.Application.Rules;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Services;

/// <summary>Builds the view the UI renders from a draft, the slot table and the catalog.</summary>
public sealed class WorkspaceViewService(
    ISlotRuleProvider slotRules,
    IWorkspaceQuoteService quotes,
    IWorkspaceQueryService queries) : IWorkspaceViewService
{
    public WorkspaceView Build(Domain.Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var quote = quotes.Quote(workspace);

        // One item per product the slot holds, priced from the quote rather than from the assignment:
        // a product that has left the catalog has no price, and saying so is the quote's job.
        var slots = slotRules.All
            .Select(rule => new AssignableSlot(
                rule.Slot,
                rule.DisplayName,
                rule.MaxQuantity,
                [.. queries.AssignmentsFor(workspace, rule.Slot).Select(assignment => ItemFor(assignment, quote))]))
            .ToArray();

        return new WorkspaceView(
            workspace.Id.Value,
            workspace.State,
            workspace.Version,
            slots,
            workspace.DeliveryAddress,
            queries.TotalUnits(workspace),
            queries.IsEmpty(workspace),
            quote,
            [.. slotRules.Mandatory
                .Where(rule => queries.AssignmentsFor(workspace, rule.Slot).Count == 0)
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
