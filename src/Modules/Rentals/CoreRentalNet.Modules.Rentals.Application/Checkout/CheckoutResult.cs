using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Rentals.Application.Checkout;

/// <summary>What a checkout produced: the order, its first invoice, and the one-time access token.</summary>
public sealed record CheckoutResult(
    string RentalNumber,
    string RawAccessToken,
    string InvoiceNumber,
    Money FirstInvoiceTotal,
    DateOnly PlacedOn,
    DateOnly DeliveryScheduledFor,
    bool WasAlreadyPlaced);
