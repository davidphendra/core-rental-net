using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Rentals.Domain.Invoices;

/// <summary>A frozen copy of an order line as it stood when the invoice was issued.</summary>
public sealed class InvoiceLine
{
    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public Money UnitMonthlyPrice { get; set; } = new Money(0m, Currencies.Idr);

    public Money LineTotal { get; set; } = new Money(0m, Currencies.Idr);
}
