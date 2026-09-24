using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace CoreRentalNet.Host.AiBuilder;

/// <summary>How far apart a run's options have to be before they are shown as a range at all.</summary>
/// <remarks>
/// <para>
/// A suggestion is a range - the affordable end and the premium end - so options that sit on top of each
/// other are not a range, however valid each one is on its own. This is the factor that decides.
/// </para>
/// <para>
/// It is configuration rather than a constant because it is a tuning knob for the evaluation tier: whether
/// 1.5 is the right distance is a measurement, and changing it should not be a rebuild.
/// </para>
/// </remarks>
internal sealed record SuggestionSpread(decimal Factor)
{
    /// <summary>What a deployment that has tuned nothing gets.</summary>
    public const decimal DefaultFactor = 1.5m;

    private const string Key = "Ai:SpreadFactor";

    /// <summary>Whether two totals are far enough apart to be shown as the ends of a range.</summary>
    public bool Holds(decimal cheapest, decimal dearest) => dearest >= cheapest * Factor;

    /// <summary>
    /// The configured factor, or the default when it is absent or unusable.
    /// </summary>
    /// <remarks>
    /// Read as text and parsed here rather than through the binder, and a value that cannot be used falls back
    /// rather than throwing: a typo in a tuning knob must not stop the application from starting, which is a
    /// failure nobody would connect to the setting that caused it. A factor below one is refused for the same
    /// reason - it can only ever accept everything, so it is a mistake rather than a choice.
    /// </remarks>
    public static SuggestionSpread From(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return decimal.TryParse(
                configuration[Key],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var factor)
            && factor >= 1m
                ? new SuggestionSpread(factor)
                : new SuggestionSpread(DefaultFactor);
    }
}
