using System.Collections.Immutable;
using System.Globalization;
using CoreRentalNet.CatalogIngestion.Chunking;
using Microsoft.Extensions.Configuration;

namespace CoreRentalNet.CatalogIngestion;

/// <summary>Turns the tool's configuration into <see cref="IngestionSettings"/>, or refuses the run.</summary>
/// <remarks>
/// <para>
/// <b>An absent key takes its default; a value that is present but unusable stops the run.</b> The two are not
/// the same: an omission means "use what you would", while a typo means the operator tried to set a value and
/// the tool must not quietly substitute another. Nothing else records what a run used, so a silently corrected
/// value would leave no trace to find later.
/// </para>
/// <para>
/// <b>It resolves paths against a base directory it is given</b> rather than finding one itself, so it is a
/// pure function of its inputs: <see cref="ProjectDirectory.Resolve"/> owns the filesystem walk, and a test
/// can pass any directory it likes.
/// </para>
/// </remarks>
internal static class ConfigurationReader
{
    /// <summary>The breakpoint strategies the library implements, in their canonical spellings.</summary>
    /// <remarks>Immutable and static, so a read does not allocate a list and no caller can alter the set.</remarks>
    private static readonly ImmutableArray<string> s_thresholdTypes =
        ["Percentile", "StandardDeviation", "InterQuartile", "Gradient"];

    /// <summary>The keys whose value may be repeated in a message. Anything else is hidden.</summary>
    /// <remarks>
    /// <b>Default-deny.</b> A key added later — a token, a connection string, anything secret — is not echoed
    /// unless someone deliberately allows it here, so a future secret cannot reach an operator's log line by
    /// being forgotten.
    /// </remarks>
    private static readonly ImmutableHashSet<string> s_safeToRepeat = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "Llm:Server",
        "Llm:Model",
        "Embedding:Width",
        "Catalog:FilePath",
        "Database:Path",
        "Chunker:TokenLimit",
        "Chunker:BufferSize",
        "Chunker:ThresholdType",
        "Chunker:ThresholdAmount",
        "Chunker:TargetChunkCount",
        "Chunker:MinChunkChars",
        "Chunker:MaxOverrunChars");

    /// <summary>The settings this configuration describes.</summary>
    /// <param name="configuration">The keys to read.</param>
    /// <param name="baseDirectory">What a relative path is relative to.</param>
    /// <exception cref="ArgumentException">A key was set to a value this tool cannot use.</exception>
    public static IngestionSettings Read(IConfiguration configuration, string baseDirectory)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

        return new IngestionSettings(
            ResolvedPath(configuration, "Catalog:FilePath", IngestionSettings.DefaultCataloguePath, baseDirectory),
            ResolvedPath(configuration, "Database:Path", IngestionSettings.DefaultDatabasePath, baseDirectory),
            Server(configuration, "Llm:Server", IngestionSettings.DefaultLlmServer),
            Text(configuration, "Llm:Model", IngestionSettings.DefaultLlmModel),
            Positive(configuration, "Embedding:Width", IngestionSettings.DefaultWidth),
            Chunker(configuration));
    }

    /// <summary>The splitter's seven values, of which only the exact-chunk-count override may be absent.</summary>
    private static ChunkerSettings Chunker(IConfiguration configuration)
        => new(
            Positive(configuration, "Chunker:TokenLimit", ChunkerSettings.DefaultTokenLimit),
            Whole(configuration, "Chunker:BufferSize", ChunkerSettings.DefaultBufferSize),
            ThresholdType(configuration),
            PositiveNumber(configuration, "Chunker:ThresholdAmount", ChunkerSettings.DefaultThresholdAmount),
            OptionalPositive(configuration, "Chunker:TargetChunkCount"),
            Whole(configuration, "Chunker:MinChunkChars", ChunkerSettings.DefaultMinChunkChars),
            Whole(configuration, "Chunker:MaxOverrunChars", ChunkerSettings.DefaultMaxOverrunChars));

    /// <summary>A required-looking text value, or the default when the key is absent.</summary>
    private static string Text(IConfiguration configuration, string key, string fallback)
    {
        var stated = configuration[key];

        if (stated is null)
        {
            return fallback;
        }

        return string.IsNullOrWhiteSpace(stated)
            ? throw new ArgumentException($"{key} was set to a blank value. Remove it to take the default, or give it a value.")
            : stated.Trim();
    }

    /// <summary>The embedding server, which has to be an absolute URL this tool can call.</summary>
    private static string Server(IConfiguration configuration, string key, string fallback)
    {
        var server = Text(configuration, key, fallback);

        return Uri.TryCreate(server, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                ? server
                : throw new ArgumentException($"{key} must be an absolute http or https URL, but was {Stated(key, server)}.");
    }

    /// <summary>A whole number greater than zero, or the default when the key is absent.</summary>
    private static int Positive(IConfiguration configuration, string key, int fallback)
        => Whole(configuration, key, fallback, minimum: 1);

    /// <summary>A whole number of zero or more, which is what a buffer and a minimum may be.</summary>
    private static int Whole(IConfiguration configuration, string key, int fallback)
        => Whole(configuration, key, fallback, minimum: 0);

    /// <summary>A number greater than zero, such as a breakpoint amount.</summary>
    private static double PositiveNumber(IConfiguration configuration, string key, double fallback)
    {
        var stated = configuration[key];

        if (stated is null)
        {
            return fallback;
        }

        return double.TryParse(stated, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : throw new ArgumentException($"{key} must be a positive number, but was {Stated(key, stated)}.");
    }

    /// <summary>The one value whose absence is meaningful rather than defaulted: unset means threshold mode.</summary>
    private static int? OptionalPositive(IConfiguration configuration, string key)
    {
        var stated = configuration[key];

        if (stated is null)
        {
            return null;
        }

        return int.TryParse(stated, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : throw new ArgumentException($"{key} must be a positive whole number when it is set, but was {Stated(key, stated)}.");
    }

    /// <summary>One of a fixed set of spellings, returned canonically whatever case was written.</summary>
    private static string ThresholdType(IConfiguration configuration)
    {
        var stated = configuration["Chunker:ThresholdType"];

        if (stated is null)
        {
            return ChunkerSettings.DefaultThresholdType;
        }

        return s_thresholdTypes.FirstOrDefault(candidate => string.Equals(candidate, stated.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException(
                $"Chunker:ThresholdType must be one of {string.Join(", ", s_thresholdTypes)}, but was {Stated("Chunker:ThresholdType", stated)}.");
    }

    /// <summary>A path as given, unless it is relative, in which case it is relative to the given directory.</summary>
    private static string ResolvedPath(IConfiguration configuration, string key, string fallback, string baseDirectory)
    {
        var stated = Text(configuration, key, fallback);

        return Path.IsPathRooted(stated) ? stated : Path.Combine(baseDirectory, stated);
    }

    /// <summary>The reader the whole-number values share, so their arithmetic and refusal are written once.</summary>
    private static int Whole(IConfiguration configuration, string key, int fallback, int minimum)
    {
        var stated = configuration[key];

        if (stated is null)
        {
            return fallback;
        }

        return int.TryParse(stated, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value >= minimum
            ? value
            : throw new ArgumentException($"{key} must be a whole number of {minimum} or more, but was {Stated(key, stated)}.");
    }

    /// <summary>The stated value, quoted, unless the key is one whose value must not be repeated.</summary>
    private static string Stated(string key, string value)
        => s_safeToRepeat.Contains(key) ? $"'{value}'" : "(hidden)";
}
