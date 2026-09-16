using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Rentals.Application.Commands.PlaceOrder;
using CoreRentalNet.Modules.Rentals.Application.Rules;
using CoreRentalNet.Modules.Rentals.Application.Services;
using CoreRentalNet.Modules.Workspace.Application.Contracts.Conversion;
using CoreRentalNet.Modules.Rentals.Domain.Persistence;

namespace CoreRentalNet.Modules.Rentals.Application.Commands.Checkout;

/// <summary>
/// Turns a draft into an order.
/// </summary>
/// <remarks>
/// The order in which this happens is the design. Everything that can refuse happens
/// <em>before</em> the draft is converted, so a customer whose workspace has an unavailable item,
/// or who forgot the address, or who mistyped the phrase, still has a draft they can fix. Only
/// once the order is priced and ready is the draft made terminal.
/// </remarks>
public sealed class CheckoutCommandHandler(
    IConvertWorkspaceToOrder converter,
    IProductCatalog catalog,
    IRentalRepository rentals,
    IPlaceOrder placeOrder,
    ICheckoutConfirmation confirmation) : ICheckoutCommandHandler
{
    /// <summary>The shortest address the order will accept, matching the draft's own rule.</summary>
    private const int AddressMinimumLength = 5;

    public async Task<CheckoutResult> CheckoutAsync(
        CheckoutCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // The demonstration gate, re-checked here and not only in the browser.
        DemoConfirmation.EnsureSatisfied(command.Confirmation);

        var conversion = await DescribeAsync(command, cancellationToken).ConfigureAwait(false);
        var address = RequireDeliveryAddress(conversion);

        // If this workspace already became an order, hand back that order rather than a second one.
        if (await AlreadyPlacedAsync(conversion.WorkspaceId, cancellationToken).ConfigureAwait(false) is { } existing)
        {
            return existing;
        }

        // Price it from the catalog, and refuse visibly if anything has gone.
        var lines = PriceLines(conversion);

        // Now, and only now, the draft becomes terminal.
        var converted = await converter
            .ConvertAsync(command.DraftToken, cancellationToken)
            .ConfigureAwait(false);

        if (converted.WasAlreadyConverted)
        {
            // Another attempt got here first; return its order.
            return await RacedOrderAsync(converted, cancellationToken).ConfigureAwait(false);
        }

        var result = await placeOrder
            .PlaceAsync(new PlaceOrderRequest(converted.WorkspaceId, lines, address), cancellationToken)
            .ConfigureAwait(false);

        return Placed(result);
    }

    /// <summary>Reads the draft and refuses it before anything else is asked of it.</summary>
    private async Task<WorkspaceConversion> DescribeAsync(CheckoutCommand command, CancellationToken cancellationToken)
    {
        var conversion = await converter
            .DescribeAsync(command.DraftToken, cancellationToken)
            .ConfigureAwait(false);

        EnsureSomethingToOrder(conversion);

        return conversion;
    }

    private static CheckoutResult Placed(PlaceOrderResult result)
        => new(
            result.RentalNumber,
            result.RawAccessToken,
            result.InvoiceNumber,
            result.FirstInvoiceTotal,
            result.PlacedOn,
            result.DeliveryScheduledFor,
            WasAlreadyPlaced: false);

    /// <summary>The order this workspace already became, or null when it became none.</summary>
    private async Task<CheckoutResult?> AlreadyPlacedAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var existing = await rentals
            .FindByWorkspaceIdAsync(workspaceId, cancellationToken)
            .ConfigureAwait(false);

        return existing is null ? null : confirmation.AlreadyPlaced(existing);
    }

    /// <summary>Another attempt converted the draft first, so its order is the one to return.</summary>
    private async Task<CheckoutResult> RacedOrderAsync(WorkspaceConversion converted, CancellationToken cancellationToken)
    {
        var raced = await rentals
            .FindByWorkspaceIdAsync(converted.WorkspaceId, cancellationToken)
            .ConfigureAwait(false);

        return raced is not null
            ? confirmation.AlreadyPlaced(raced)
            : throw new DomainRuleViolationException("This workspace has already been rented.");
    }

    /// <summary>There has to be something to order before anything else is asked of the draft.</summary>
    private static void EnsureSomethingToOrder(WorkspaceConversion conversion)
    {
        if (conversion.Lines.Count == 0)
        {
            throw new DomainRuleViolationException("Add at least one item to your workspace before renting it.");
        }
    }

    /// <summary>Somewhere to deliver it: the order refuses a blank or too-short one.</summary>
    private static string RequireDeliveryAddress(WorkspaceConversion conversion)
    {
        var address = conversion.DeliveryAddress;

        if (string.IsNullOrWhiteSpace(address) || address.Length < AddressMinimumLength)
        {
            throw new DomainRuleViolationException("Add a delivery address before renting.");
        }

        return address;
    }

    /// <summary>
    /// Prices every line from the catalog rather than from the draft: the client sends no amount, and
    /// a product that has left the catalog is refused by name rather than priced from memory
    ///.
    /// </summary>
    private IReadOnlyList<OrderLineRequest> PriceLines(WorkspaceConversion conversion)
    {
        var lines = new List<OrderLineRequest>(conversion.Lines.Count);

        foreach (var line in conversion.Lines)
        {
            var price = catalog.Find(line.Sku)
                ?? throw new DomainRuleViolationException(
                    $"'{line.Sku}' is no longer in the catalog. Remove it from your workspace before renting.");

            lines.Add(new OrderLineRequest(price.Sku, price.Name, line.Quantity, price.MonthlyPrice));
        }

        return lines;
    }
}
