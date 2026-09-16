using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Rentals.Domain.Numbering;

namespace CoreRentalNet.Modules.Rentals.Domain.Rentals;

/// <summary>The customer-facing order reference, for example <c>CR-2026-0001</c>.</summary>
public sealed record RentalNumber
{
    public const string Prefix = "CR";

    private RentalNumber(int year, int sequence)
    {
        Year = year;
        Sequence = sequence;
        Value = NumberFormat.Format(Prefix, year, sequence);
    }

    public int Year { get; }

    public int Sequence { get; }

    public string Value { get; }

    /// <summary>Builds a number from its year and sequence.</summary>
    public static RentalNumber Of(int year, int sequence) => new(year, sequence);

    /// <summary>Parses a customer-facing reference.</summary>
    /// <exception cref="DomainRuleViolationException">The text is not a rental number.</exception>
    public static RentalNumber Parse(string value)
        => TryParse(value, out var number)
            ? number
            : throw new DomainRuleViolationException($"'{value}' is not a rental number.");

    /// <summary>Parses a reference without throwing; returns false when it is not one.</summary>
    public static bool TryParse(string? value, out RentalNumber number)
    {
        number = null!;

        if (!NumberFormat.TryParse(value, Prefix, out var year, out var sequence))
        {
            return false;
        }

        number = new RentalNumber(year, sequence);
        return true;
    }

    public override string ToString() => Value;
}
