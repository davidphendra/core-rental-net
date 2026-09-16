using CoreRentalNet.Modules.Rentals.Domain.Invoices;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.Domain.Numbering;

/// <summary>Which yearly number sequence a number is reserved from.</summary>
public enum SequenceKind
{
    Rental = 1,
    Invoice = 2,
}
