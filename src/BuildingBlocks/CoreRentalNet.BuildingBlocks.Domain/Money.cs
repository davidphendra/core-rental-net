namespace CoreRentalNet.BuildingBlocks.Domain;

/// <summary>
/// A monetary amount in a single currency.
/// </summary>
/// <remarks>
/// A plain record: a value is just its amount and its currency. The rules — an amount is
/// never negative, only IDR is charged, and rounding happens once — live in <c>IMoneyService</c>.
/// </remarks>
/// <param name="Amount">The face amount, in the currency named beside it.</param>
/// <param name="Currency">The ISO 4217 code of the currency the amount is stated in.</param>
public sealed record Money(decimal Amount, string Currency);
