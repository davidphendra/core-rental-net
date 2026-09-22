using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Queries.Views;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Services;

/// <summary>Prices a draft from the catalogService, on every read.</summary>
public sealed class WorkspaceQuoteService(IMoneyService money, IProductCatalogService catalogService) : IWorkspaceQuoteService
{
    public WorkspaceQuote Quote(Domain.Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var lines = new List<QuoteLine>(workspace.Assignments.Count);

        foreach (var assignment in workspace.Assignments)
        {
            var price = catalogService.Find(assignment.Sku);

            lines.Add(price is null
                ? new QuoteLine(assignment.Slot, assignment.Sku, "(no longer available)", assignment.Quantity, null, null, null, false)
                : new QuoteLine(
                    assignment.Slot,
                    price.Sku,
                    price.Name,
                    assignment.Quantity,
                    price.MonthlyPrice,
                    money.Round(money.Times(price.MonthlyPrice, assignment.Quantity)),
                    price.ImagePath,
                    price.ImageAvailable));
        }

        var subtotal = lines.Count == 0 || lines.All(line => line.LineMonthlyTotal is null)
            ? new Money(0m, Currencies.Idr)
            : money.Round(money.Sum(lines.Where(line => line.LineMonthlyTotal is not null).Select(line => line.LineMonthlyTotal!)));

        return new WorkspaceQuote(lines, subtotal);
    }
}
