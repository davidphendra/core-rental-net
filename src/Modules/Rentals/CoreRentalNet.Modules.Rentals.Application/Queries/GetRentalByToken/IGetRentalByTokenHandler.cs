using CoreRentalNet.Modules.Rentals.Application.Queries.Views;

namespace CoreRentalNet.Modules.Rentals.Application.Queries.GetRentalByToken;

/// <summary>
/// Looks an order up by its access token.
/// </summary>
/// <remarks>
/// Returns null for an unknown token rather than throwing, and takes no order number, so there is
/// nothing to enumerate: guessing a number discloses nothing (matrix ORD-02, SEC-06).
/// </remarks>
public interface IGetRentalByTokenHandler
{
    Task<RentalView?> HandleAsync(GetRentalByTokenQuery query, CancellationToken cancellationToken = default);
}
