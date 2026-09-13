using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Rentals.Application.Views;
using CoreRentalNet.Modules.Rentals.Domain;

namespace CoreRentalNet.Modules.Rentals.Application.Queries;

public sealed record GetRentalByToken(string AccessToken);

/// <summary>
/// Looks an order up by its access token.
/// </summary>
/// <remarks>
/// Returns null for an unknown token rather than throwing, and takes no order number, so there is
/// nothing to enumerate: guessing a number discloses nothing (matrix ORD-02, SEC-06).
/// </remarks>
/// <summary>The order a token opens, or nothing when the token matches none.</summary>
public interface IGetRentalByToken
{
    Task<RentalView?> HandleAsync(GetRentalByToken query, CancellationToken cancellationToken = default);
}

public sealed class GetRentalByTokenHandler(IRentalRepository rentals, TimeProvider clock) : IGetRentalByToken
{
    public async Task<RentalView?> HandleAsync(GetRentalByToken query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (string.IsNullOrWhiteSpace(query.AccessToken))
        {
            return null;
        }

        var rental = await rentals
            .FindByTokenAsync(AccessToken.FromRawToken(query.AccessToken), cancellationToken)
            .ConfigureAwait(false);

        if (rental is null)
        {
            return null;
        }

        var today = BusinessTime.Today(clock);
        var period = rental.PeriodContaining(today > rental.AnchorDate ? today : rental.AnchorDate);

        return new RentalView(
            rental.Number.Value,
            rental.Status,
            rental.DeliveryAddress,
            rental.PlacedOn,
            rental.AnchorDate,
            rental.MonthlyTotal,
            rental.DeliveryFee,
            rental.TotalUnits,
            rental.DeliveryScheduledFor,
            rental.ActivatedOn,
            rental.CancellationRequestedOn,
            rental.EndsOn,
            period.Start,
            period.EndExclusive,
            rental.Lines
                .Select(line => new RentalLineView(line.Sku, line.Name, line.Quantity, line.UnitMonthlyPrice, line.LineTotal))
                .ToArray());
    }
}
