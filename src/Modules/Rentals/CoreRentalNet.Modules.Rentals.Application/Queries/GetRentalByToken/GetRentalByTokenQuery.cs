namespace CoreRentalNet.Modules.Rentals.Application.Queries.GetRentalByToken;

/// <summary>The order lookup request: the raw token from the customer's cookie.</summary>
public sealed record GetRentalByTokenQuery(string AccessToken);
