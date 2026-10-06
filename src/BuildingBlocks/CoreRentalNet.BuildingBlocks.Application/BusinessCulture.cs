using System.Globalization;
using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.BuildingBlocks.Application;

/// <summary>
/// The one culture every number and date this application writes is written in.
/// </summary>
/// <remarks>
/// <para>
/// The culture supplies what is regional: the currency symbol, the group separator and the decimal
/// separator. The number of decimals does not come from it, because that is a property of the money
/// and not of the machine. The CLDR data behind <see cref="CultureInfo"/> reports two decimals for
/// en-ID on Linux, where the application runs, and zero on macOS, where it was written and tested,
/// so a locale read draws one invoice two ways. <see cref="Currencies.IdrDecimals"/> is the number
/// used instead, and one amount then reads the same everywhere.
/// </para>
/// <para>
/// The culture named is expected to name the settlement currency's region; the architecture suite
/// asserts that it does, so a culture that would draw dollars is caught rather than formatted.
/// </para>
/// </remarks>
public static class BusinessCulture
{
    /// <summary>The culture named, with the settlement currency's own number of decimals.</summary>
    /// <param name="name">The culture name, for example <c>en-ID</c>.</param>
    public static CultureInfo For(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var culture = new CultureInfo(name, useUserOverride: false);
        var format = (NumberFormatInfo)culture.NumberFormat.Clone();

        format.CurrencyDecimalDigits = Currencies.IdrDecimals;
        culture.NumberFormat = format;

        return culture;
    }
}
