namespace CoreRentalNet.BuildingBlocks.Domain;

/// <summary>The currencies this product understands. Core Rental bills in rupiah only.</summary>
public static class Currencies
{
    /// <summary>The ISO 4217 code for the Indonesian rupiah, the only currency charged.</summary>
    public const string Idr = "IDR";

    /// <summary>
    /// How many decimals the rupiah is written with. The rupiah has no minor unit, so none. The
    /// count belongs to the currency, not to the machine.
    /// </summary>
    /// <remarks>
    /// Stated here rather than read from the culture: the CLDR data behind <c>CultureInfo</c>
    /// reports two decimals for en-ID on Linux and zero on macOS, so a read from the locale would
    /// draw one invoice two ways and the server would disagree with the suite.
    /// </remarks>
    public const int IdrDecimals = 0;

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
