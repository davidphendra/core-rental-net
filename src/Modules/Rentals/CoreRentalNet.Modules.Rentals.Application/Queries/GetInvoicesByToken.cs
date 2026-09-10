using CoreRentalNet.Modules.Rentals.Application.Views;
using CoreRentalNet.Modules.Rentals.Domain;

namespace CoreRentalNet.Modules.Rentals.Application.Queries;

public sealed record GetInvoicesByToken(string AccessToken);

/// <summary>Every invoice raised for the order behind a token, oldest first.</summary>
public sealed class GetInvoicesByTokenHandler(IRentalRepository rentals, IInvoiceRepository invoices)
{
    public async Task<IReadOnlyList<InvoiceView>> HandleAsync(
        GetInvoicesByToken query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (string.IsNullOrWhiteSpace(query.AccessToken))
        {
            return [];
        }

        var rental = await rentals
            .FindByTokenAsync(AccessToken.FromRawToken(query.AccessToken), cancellationToken)
            .ConfigureAwait(false);

        if (rental is null)
        {
            return [];
        }

        var issued = await invoices.ListForRentalAsync(rental.Id, cancellationToken).ConfigureAwait(false);

        return issued
            .OrderBy(invoice => invoice.PeriodIndex)
            .Select(invoice => new InvoiceView(
                invoice.Number.Value,
                invoice.PeriodIndex,
                invoice.PeriodStart,
                invoice.PeriodEnd,
                invoice.Subtotal,
                invoice.TaxRate,
                invoice.TaxAmount,
                invoice.HasTaxLine,
                invoice.DeliveryFee,
                invoice.Total,
                invoice.Status,
                invoice.IssuedOn,
                invoice.PaidOn,
                invoice.Lines
                    .Select(line => new InvoiceLineView(line.Sku, line.Name, line.Quantity, line.UnitMonthlyPrice, line.LineTotal))
                    .ToArray()))
            .ToArray();
    }
}
