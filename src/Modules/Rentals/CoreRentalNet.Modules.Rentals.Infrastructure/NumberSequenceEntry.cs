namespace CoreRentalNet.Modules.Rentals.Infrastructure;

/// <summary>
/// The counter behind order and invoice numbers, one row per kind and year. Infrastructure-only:
/// the domain knows only that it can ask for the next number.
/// </summary>
public sealed class NumberSequenceEntry
{
    public int Kind { get; set; }

    public int Year { get; set; }

    public int Value { get; set; }
}
