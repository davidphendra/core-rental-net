namespace CoreRentalNet.Modules.Rentals.Application.Checkout;

/// <summary>
/// What the customer submits. Note there is no amount, no total and no number here: the only
/// things a caller may influence are which draft and what they typed (matrix CO-13).
/// </summary>
public sealed record CheckoutCommand(string DraftToken, string? Confirmation);
