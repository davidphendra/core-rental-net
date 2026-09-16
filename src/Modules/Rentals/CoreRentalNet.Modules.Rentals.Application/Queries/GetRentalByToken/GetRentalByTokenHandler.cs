using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Rentals.Application.Rentals;
using CoreRentalNet.Modules.Rentals.Application.Views;
using CoreRentalNet.Modules.Rentals.Domain.Persistence;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.Application.Queries.GetRentalByToken;

/// <summary>Answers <see cref="IGetRentalByTokenHandler"/> by assembling the view the confirmation page renders.</summary>
public sealed class GetRentalByTokenHandler(
    IRentalRepository rentals,
    IOpaqueTokenService tokens,
    IMoneyService money,
    IRentalLifecycleService lifecycle,
    TimeProvider clock) : IGetRentalByTokenHandler
{
    public async Task<RentalView?> HandleAsync(GetRentalByTokenQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (string.IsNullOrWhiteSpace(query.AccessToken))
        {
            return null;
        }

        var rental = await rentals
            .FindByTokenAsync(new AccessToken(tokens.HashOf(query.AccessToken)), cancellationToken)
            .ConfigureAwait(false);

        if (rental is null)
        {
            return null;
        }

        var today = BusinessTime.Today(clock);

        return ToView(rental, lifecycle.PeriodContaining(rental, today > rental.AnchorDate ? today : rental.AnchorDate));
    }

    private RentalView ToView(Rental rental, RentalPeriod period)
        => new(
            rental.Number.Value,
            rental.Status,
            rental.DeliveryAddress,
            rental.PlacedOn,
            rental.AnchorDate,
            lifecycle.MonthlyTotal(rental),
            rental.DeliveryFee,
            lifecycle.TotalUnits(rental),
            rental.DeliveryScheduledFor,
            rental.ActivatedOn,
            rental.CancellationRequestedOn,
            rental.EndsOn,
            period.Start,
            period.EndExclusive,
            rental.Lines
                .Select(line => new RentalLineView(
                    line.Sku,
                    line.Name,
                    line.Quantity,
                    line.UnitMonthlyPrice,
                    money.Round(money.Times(line.UnitMonthlyPrice, line.Quantity))))
                .ToArray());
}
