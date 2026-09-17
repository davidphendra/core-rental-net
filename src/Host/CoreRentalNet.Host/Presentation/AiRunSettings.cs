using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// How long a customer waits between runs, configured rather than hard-coded.
/// </summary>
/// <remarks>
/// <para>
/// Short by default, and short on purpose: the guard exists to absorb a double click, and a cooldown long
/// enough to be noticed would be a rate limit - which this is documented not to be.
/// </para>
/// <para>
/// The value is read as text and parsed here rather than through the binder, because the binder
/// <em>throws</em> on a value it cannot convert. A typo in configuration would then stop the application
/// from starting, over a cooldown, which is neither the failure anybody wants nor one they would connect
/// to the setting that caused it.
/// </para>
/// </remarks>
internal sealed record AiRunSettings(TimeSpan Cooldown)
{
    private static readonly TimeSpan Default = TimeSpan.FromSeconds(3);

    public static AiRunSettings From(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var seconds = double.TryParse(
            configuration["Ai:RunCooldownSeconds"],
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var configured) && configured >= 0
                ? configured
                : Default.TotalSeconds;

        return new AiRunSettings(TimeSpan.FromSeconds(seconds));
    }
}
