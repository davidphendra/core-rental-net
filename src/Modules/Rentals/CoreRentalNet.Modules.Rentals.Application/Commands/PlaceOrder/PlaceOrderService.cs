using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Rentals.Application.Services;
using CoreRentalNet.Modules.Rentals.Application.Rules;
using CoreRentalNet.Modules.Rentals.Domain.Invoices;
using CoreRentalNet.Modules.Rentals.Domain.Numbering;
using CoreRentalNet.Modules.Rentals.Domain.Persistence;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.Application.Commands.PlaceOrder;

/// <summary>
/// Turns a composition into an order and settles its first month.
/// </summary>
/// <remarks>
/// The raw access token is generated here and returned once; only its hash is ever stored. The
/// first invoice is issued and paid in the same transaction, because there is no way for payment
/// to fail in this application. The order itself is a plain record; its status moves
/// through <c>IRentalLifecycleService</c>.
/// </remarks>
public sealed class PlaceOrderService(
    IRentalRepository rentals,
    IInvoiceRepository invoices,
    IOpaqueTokenService tokens,
    IInvoiceService invoicing,
    IRentalLifecycleService lifecycle,
    IDeliveryPolicyService deliveries,
    INumberSequence numbers,
    IUnitOfWork unitOfWork,
    RentalsSettings settings,
    TimeProvider clock) : IPlaceOrder
{
    public async Task<PlaceOrderResult> PlaceAsync(PlaceOrderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsurePlaceable(request);

        var placedOn = BusinessTime.Today(clock);
        var rawToken = tokens.IssueRawToken();

        var rentalNumber = RentalNumber.Of(
            placedOn.Year,
            await numbers.ReserveNextAsync(SequenceKind.Rental, placedOn.Year, cancellationToken).ConfigureAwait(false));

        var rental = BuildRental(request, rentalNumber, tokens.HashOf(rawToken), placedOn);

        var invoiceNumber = InvoiceNumber.Of(
            placedOn.Year,
            await numbers.ReserveNextAsync(SequenceKind.Invoice, placedOn.Year, cancellationToken).ConfigureAwait(false));

        var firstInvoice = invoicing.IssueFor(InvoiceId.New(), invoiceNumber, rental, periodIndex: 0, settings.TaxRate, placedOn);

        invoicing.Settle(firstInvoice, placedOn);
        lifecycle.MarkPaid(rental, placedOn);

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
            deliveries.ScheduledFor(placedOn));
    }

    /// <summary>Everything that has to hold before an order is built or a number is reserved.</summary>
    private static void EnsurePlaceable(PlaceOrderRequest request)
    {
        if (request.Lines.Count == 0)
        {
            throw new DomainRuleViolationException("An order needs at least one line.");
        }

        if (string.IsNullOrWhiteSpace(request.DeliveryAddress))
        {
            throw new DomainRuleViolationException("An order needs a delivery address.");
        }

        if (request.WorkspaceId == Guid.Empty)
        {
            throw new DomainRuleViolationException("An order must remember the workspace it came from.");
        }

        var emptyLine = request.Lines.FirstOrDefault(line => line.Quantity < 1);

        if (emptyLine is not null)
        {
            throw new DomainRuleViolationException($"A line needs at least one unit, but {emptyLine.Quantity} was given.");
        }
    }

    private Rental BuildRental(PlaceOrderRequest request, RentalNumber number, string tokenHash, DateOnly placedOn)
        => new()
        {
            Id = RentalId.New(),
            WorkspaceId = request.WorkspaceId,
            Number = number,
            AccessTokenHash = tokenHash,
            DeliveryAddress = request.DeliveryAddress,
            DeliveryFee = settings.DeliveryFee,
            PlacedOn = placedOn,
            AnchorDate = placedOn,
            Status = RentalStatus.Placed,
            Version = 1,
            Lines = request.Lines.Select(line => new RentalLine
            {
                Sku = line.Sku,
                Name = line.Name,
                Quantity = line.Quantity,
                UnitMonthlyPrice = line.UnitMonthlyPrice,
            }).ToList(),
        };
}
