using System.Globalization;
using CoreRentalNet.BuildingBlocks.Application;

namespace CoreRentalNet.Host.Extentions;

/// <summary>
/// The one culture every number and date this application writes is written in.
/// </summary>
/// <remarks>
/// <para>
/// Microsoft's guidance for server applications is to set the culture explicitly rather than rely on
/// the machine's, and to format a number through <see cref="CultureInfo.CurrentCulture"/>. A server
/// runs under the invariant culture, and an amount written under it reads <c>¤400,000</c>, not the
/// amount a person in Denpasar sees. One culture serves the whole application because it bills in
/// one currency, so this is set once, here, instead of being hard-coded beside every format call.
/// </para>
/// <para>
/// The value is configuration rather than a constant, so which market a deployment writes for is a
/// settings change. The culture is built by <see cref="BusinessCulture"/>, which takes the symbol
/// and the separators from it and the number of decimals from the currency, so neither a machine's
/// own regional tweaks nor its CLDR data can change what an invoice reads.
/// </para>
/// </remarks>
internal static class BusinessCultureExtentions
{
    /// <summary>Applies the culture configuration names, if it names one, to every thread that has not chosen its own.</summary>
    public static void AddBusinessCulture(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var name = builder.Configuration["Culture:Name"];

        // Configuration names none: leave the framework's own choice alone rather than invent one
        // here, which is what a test host that never loads appsettings.json relies on.
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var culture = BusinessCulture.For(name);

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}
