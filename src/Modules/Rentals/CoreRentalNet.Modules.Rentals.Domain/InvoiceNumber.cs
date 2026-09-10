using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Rentals.Domain;

/// <summary>The invoice reference, for example <c>INV-2026-0001</c>.</summary>
public sealed record InvoiceNumber
{
    public const string Prefix = "INV";

    private InvoiceNumber(int year, int sequence)
    {
        Year = year;
        Sequence = sequence;
        Value = NumberFormat.Format(Prefix, year, sequence);
    }

    public int Year { get; }

    public int Sequence { get; }

    public string Value { get; }

    public static InvoiceNumber Of(int year, int sequence) => new(year, sequence);

    public static InvoiceNumber Parse(string value)
        => TryParse(value, out var number)
            ? number
            : throw new DomainRuleViolationException($"'{value}' is not an invoice number.");

    public static bool TryParse(string? value, out InvoiceNumber number)
    {
        number = null!;

        if (!NumberFormat.TryParse(value, Prefix, out var year, out var sequence))
        {
            return false;
        }

        number = new InvoiceNumber(year, sequence);
        return true;
    }

    public override string ToString() => Value;
}
