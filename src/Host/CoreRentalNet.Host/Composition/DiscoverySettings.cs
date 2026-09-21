using System.Globalization;

namespace CoreRentalNet.Host.Composition;

/// <summary>Where the embedding deployment is, what model it serves, and how wide its vectors are.</summary>
/// <remarks>
/// <para>
/// <b>Every value is required together or the feature is off.</b> There is no default endpoint, no default
/// deployment and no default width, for the reason <see cref="Agents.SuggestionAgentSettings"/> gives about the
/// agent: a deployment that has not been told which deployment embeds its requests has not been finished, and
/// the honest failure is a hidden section rather than a workspace built from vectors nothing can explain.
/// </para>
/// <para>
/// <b>The deployment and the model are two settings because they are two facts.</b> The deployment is what the
/// call is made against; the model is what the index RECORDS, and the freshness check compares that. An alias
/// can be repointed at a new model without its name changing, so comparing the deployment would call an index
/// current when the vectors underneath it no longer came from the same model. The ingestion tool takes the same
/// two, for the same reason.
/// </para>
/// <para>
/// <b>The width is configuration and the index records it.</b> A deployment that changes the width here changes
/// nothing about the stored index — the freshness check compares the two and refuses the mismatch, which is what
/// turns a silent misalignment into a message naming the tool to run.
/// </para>
/// </remarks>
internal sealed record DiscoverySettings(
    string ProjectEndpoint,
    string Deployment,
    string ModelId,
    int Width,
    int PerBucket,
    double BoostWeight,
    double HalfLifeDays)
{
    /// <summary>What a deployment that has tuned nothing gets: the width the index was built at and two per bucket.</summary>
    public const int DefaultWidth = 512;

    public const int DefaultPerBucket = 2;

    /// <summary>How much a customer's past choices may move a product within its bucket. Zero switches it off.</summary>
    public const double DefaultBoostWeight = 0.1;

    /// <summary>How long a selection speaks for, in days. After this, half of it is forgotten.</summary>
    public const double DefaultHalfLifeDays = 30;

    /// <summary>True when this deployment has been told where to embed.</summary>
    public bool IsConfigured
        => !string.IsNullOrWhiteSpace(ProjectEndpoint)
            && !string.IsNullOrWhiteSpace(Deployment)
            && !string.IsNullOrWhiteSpace(ModelId)
            && Width > 0;

    /// <summary>
    /// A tuning knob, or the default when it cannot be used.
    /// </summary>
    /// <remarks>
    /// Read as text and parsed here rather than through the binder, and an unusable value falls back rather than
    /// throwing — the rule <c>SuggestionSpread.From</c> already sets. A typo in a tuning knob must not stop the
    /// application from starting, which is a failure nobody would connect to the setting that caused it. A value
    /// at or below zero is refused for the same reason: a negative half-life would decay upwards.
    /// </remarks>
    private static double Tunable(string? configured, double fallback)
        => double.TryParse(configured, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : fallback;

    public static DiscoverySettings From(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new DiscoverySettings(
            configuration["Discovery:ProjectEndpoint"]?.Trim() ?? string.Empty,
            configuration["Discovery:Deployment"]?.Trim() ?? string.Empty,
            configuration["Discovery:ModelId"]?.Trim() ?? string.Empty,
            configuration.GetValue("Discovery:Width", DefaultWidth),
            configuration.GetValue("Discovery:PerBucket", DefaultPerBucket),
            Tunable(configuration["Discovery:BoostWeight"], DefaultBoostWeight),
            Tunable(configuration["Discovery:HalfLifeDays"], DefaultHalfLifeDays));
    }
}
