using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Rentals.Domain;

/// <summary>
/// One place for the <c>PREFIX-YYYY-NNNN</c> shape, so the order number and the invoice number
/// cannot drift apart. The number is a label a person reads out loud, not a secret: access is
/// controlled by the token, so a guessable number discloses nothing (matrix SEC-06).
/// </summary>
internal static class NumberFormat
{
    private const int MinYear = 2000;
    private const int MaxYear = 2999;
    private const int MaxSequence = 9999;

    public static string Format(string prefix, int year, int sequence)
    {
        ValidateYear(year);

        if (sequence < 1 || sequence > MaxSequence)
        {
            throw new DomainRuleViolationException(
                $"A sequence number runs from 1 to {MaxSequence}, but {sequence} was given.");
        }

        return $"{prefix}-{year:D4}-{sequence:D4}";
    }

    public static bool TryParse(string? value, string prefix, out int year, out int sequence)
    {
        year = 0;
        sequence = 0;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Trim().Split('-');

        if (parts.Length != 3 || !string.Equals(parts[0], prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!int.TryParse(parts[1], out year) || year is < MinYear or > MaxYear)
        {
            return false;
        }

        if (!int.TryParse(parts[2], out sequence) || sequence is < 1 or > MaxSequence)
        {
            return false;
        }

        return true;
    }

    private static void ValidateYear(int year)
    {
        if (year is < MinYear or > MaxYear)
        {
            throw new DomainRuleViolationException($"A year must be between {MinYear} and {MaxYear}, but {year} was given.");
        }
    }
}
