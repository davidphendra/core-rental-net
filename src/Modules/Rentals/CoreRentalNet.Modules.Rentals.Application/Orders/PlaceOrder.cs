using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Rentals.Domain;

namespace CoreRentalNet.Modules.Rentals.Application.Orders;

/// <summary>One line of a frozen workspace composition, priced by the catalog.</summary>
public sealed record OrderLineRequest(string Sku, string Name, int Quantity, Money UnitMonthlyPrice);

/// <summary>
/// Everything the order needs. Note what is absent: no price total, no number and no status.
/// The caller supplies what was rented and where it goes, and this module decides the rest.
/// </summary>
public sealed record PlaceOrderRequest(
    Guid WorkspaceId,
    IReadOnlyList<OrderLineRequest> Lines,
    string DeliveryAddress);

public sealed record PlaceOrderResult(
    Guid RentalId,
    string RentalNumber,
    string RawAccessToken,
    string InvoiceNumber,
    Money FirstInvoiceTotal,
    DateOnly PlacedOn,
    DateOnly DeliveryScheduledFor);

public interface IPlaceOrder
{
    Task<PlaceOrderResult> PlaceAsync(PlaceOrderRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Turns a composition into an order and settles its first month.
/// </summary>
/// <remarks>
/// The raw access token is generated here and returned once; only its hash is ever stored. The
/// first invoice is issued and paid in the same transaction, because there is no way for payment
/// to fail in this application.
/// </remarks>
public sealed class PlaceOrderService(
    IRentalRepository rentals,
    IInvoiceRepository invoices,
    INumberSequence numbers,
    IUnitOfWork unitOfWork,
    RentalsSettings settings,
    TimeProvider clock) : IPlaceOrder
{
    public async Task<PlaceOrderResult> PlaceAsync(PlaceOrderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Lines.Count == 0)
        {
            throw new DomainRuleViolationException("An order needs at least one line.");
        }

        if (string.IsNullOrWhiteSpace(request.DeliveryAddress))
        {
            throw new DomainRuleViolationException("An order needs a delivery address.");
        }

        var placedOn = BusinessTime.Today(clock);

        var rawToken = AccessToken.IssueRawToken();
        var token = AccessToken.FromRawToken(rawToken);

        var lines = request.Lines
            .Select(line => new RentalLine(line.Sku, line.Name, line.Quantity, line.UnitMonthlyPrice))
            .ToArray();

        var rentalNumber = RentalNumber.Of(
            placedOn.Year,
            await numbers.ReserveNextAsync(SequenceKind.Rental, placedOn.Year, cancellationToken).ConfigureAwait(false));

        var rental = Rental.Place(
            RentalId.New(),
            rentalNumber,
            token.Hash,
            request.DeliveryAddress,
            settings.DeliveryFee,
            lines,
            placedOn);

        var invoiceNumber = InvoiceNumber.Of(
            placedOn.Year,
            await numbers.ReserveNextAsync(SequenceKind.Invoice, placedOn.Year, cancellationToken).ConfigureAwait(false));

        var firstInvoice = Invoice.IssueFor(InvoiceId.New(), invoiceNumber, rental, periodIndex: 0, settings.TaxRate, placedOn);

        firstInvoice.Settle(placedOn);
        rental.MarkPaid(placedOn);

        await rentals.AddAsync(rental, cancellationToken).ConfigureAwait(false);
        await invoices.AddAsync(firstInvoice, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new PlaceOrderResult(
            rental.Id.Value,
            rental.Number.Value,
            rawToken,
            firstInvoice.Number.Value,
            firstInvoice.Total,
            placedOn,
            DeliveryPolicy.ScheduledFor(placedOn));
    }
}
