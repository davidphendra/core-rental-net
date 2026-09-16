using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.Modules.Rentals.Application.Services;
using CoreRentalNet.Modules.Rentals.Application.Queries.Views;
using CoreRentalNet.Modules.Rentals.Domain.Invoices;
using CoreRentalNet.Modules.Rentals.Domain.Persistence;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.Application.Queries.GetInvoicesByToken;

/// <summary>Answers <see cref="IGetInvoicesByTokenHandler"/> from the order the token opens, newest period first.</summary>
public sealed class GetInvoicesByTokenHandler(
    IRentalRepository rentals,
    IInvoiceRepository invoices,
    IOpaqueTokenService tokens,
    IInvoiceService invoicing) : IGetInvoicesByTokenHandler
{
    public async Task<IReadOnlyList<InvoiceView>> HandleAsync(
        GetInvoicesByTokenQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (string.IsNullOrWhiteSpace(query.AccessToken))
        {
            return [];
        }

        var rental = await rentals
            .FindByTokenAsync(new AccessToken(tokens.HashOf(query.AccessToken)), cancellationToken)
            .ConfigureAwait(false);

        if (rental is null)
        {
            return [];
        }

        var issued = await invoices.ListForRentalAsync(rental.Id, cancellationToken).ConfigureAwait(false);

        return issued
            .OrderBy(invoice => invoice.PeriodIndex)
            .Select(ToView)
            .ToArray();
    }

    private InvoiceView ToView(Invoice invoice)
        => new(
            invoice.Number.Value,
            invoice.PeriodIndex,
            invoice.PeriodStart,
            invoice.PeriodEnd,
            invoice.Subtotal,
            invoice.TaxRate,
            invoice.TaxAmount,
            invoicing.HasTaxLine(invoice),
            invoice.DeliveryFee,
            invoice.Total,
            invoice.Status,
            invoice.IssuedOn,
            invoice.PaidOn,
            invoice.Lines
                .Select(line => new InvoiceLineView(line.Sku, line.Name, line.Quantity, line.UnitMonthlyPrice, line.LineTotal))
                .ToArray());
}
