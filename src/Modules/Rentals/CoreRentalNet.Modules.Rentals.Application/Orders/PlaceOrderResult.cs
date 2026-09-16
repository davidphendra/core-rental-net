using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Rentals.Application.Orders;

/// <summary>What placing an order produced, including the raw token only its first holder ever sees.</summary>
public sealed record PlaceOrderResult(
    Guid RentalId,
    string RentalNumber,
    string RawAccessToken,
    string InvoiceNumber,
    Money FirstInvoiceTotal,
    DateOnly PlacedOn,
    DateOnly DeliveryScheduledFor);
