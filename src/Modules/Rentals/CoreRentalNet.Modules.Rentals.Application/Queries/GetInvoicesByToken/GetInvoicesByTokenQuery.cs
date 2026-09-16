namespace CoreRentalNet.Modules.Rentals.Application.Queries.GetInvoicesByToken;

/// <summary>The statement request: the raw token from the customer's cookie.</summary>
public sealed record GetInvoicesByTokenQuery(string AccessToken);
