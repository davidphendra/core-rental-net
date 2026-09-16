namespace CoreRentalNet.BuildingBlocks.Domain;

/// <summary>The currencies this product understands. Core Rental bills in rupiah only.</summary>
public static class Currencies
{
    /// <summary>The ISO 4217 code for the Indonesian rupiah, the only currency charged.</summary>
    public const string Idr = "IDR";

    /// <summary>
    /// True when the text has the shape ISO 4217 gives a currency: three uppercase letters.
    /// </summary>
    /// <remarks>
    /// A shape test, not a membership test: the catalog may name a currency this application cannot
    /// yet charge, and the loader refuses that by comparing against the settlement currency rather
    /// than by pretending the code is misspelled.
    /// </remarks>
    public static bool IsWellFormed(string? code)
        => code is { Length: 3 } && code.All(char.IsAsciiLetterUpper);
}
