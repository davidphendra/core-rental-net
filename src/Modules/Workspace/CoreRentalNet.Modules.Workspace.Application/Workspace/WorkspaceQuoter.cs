using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Workspace;

/// <summary>Prices a draft from the catalog. The single place a workspace total is produced.</summary>
public static class WorkspaceQuoter
{
    public static WorkspaceQuote Quote(Domain.Workspace workspace, IDefineProductPrices prices)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(prices);

        var lines = new List<QuoteLine>(workspace.Assignments.Count);

        foreach (var assignment in workspace.Assignments)
        {
            var price = prices.FindPrice(assignment.Sku);

            lines.Add(price is null
                ? new QuoteLine(assignment.Slot, assignment.Sku, "(no longer available)", assignment.Quantity, null, null)
                : new QuoteLine(
                    assignment.Slot,
                    price.Sku,
                    price.Name,
                    assignment.Quantity,
                    price.MonthlyPrice,
                    price.MonthlyPrice.Times(assignment.Quantity).Round()));
        }

        var subtotal = lines.Count == 0 || lines.All(line => line.LineMonthlyTotal is null)
            ? Money.Idr(0m)
            : Money.Sum(lines.Where(line => line.LineMonthlyTotal is not null).Select(line => line.LineMonthlyTotal!)).Round();

        return new WorkspaceQuote(lines, subtotal);
    }
}
