namespace CoreRentalNet.Modules.Rentals.Domain.Invoices;

/// <summary>Whether an invoice is still owed or settled.</summary>
public enum InvoiceStatus
{
    Open = 1,
    Paid = 2,
}
