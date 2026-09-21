namespace CoreRentalNet.CatalogIngestion.Chunking;

/// <summary>How a product's text is split before its pieces are embedded.</summary>
/// <remarks>
/// <para>
/// <b>Every value here moves a chunk boundary</b>, which is why they are configuration rather than constants:
/// the corpus decides which values suit it, and a value fixed in code could only be changed by rebuilding
/// the tool.
/// </para>
/// <para>
/// <see cref="TargetChunkCount"/> is the one value whose absence is meaningful rather than defaulted. When it
/// is set, the library aims for that exact count and the threshold pair is not consulted — the two are
/// alternative modes, not seven independent numbers.
/// </para>
/// </remarks>
internal sealed record ChunkerSettings(
    int TokenLimit,
    int BufferSize,
    string ThresholdType,
    double ThresholdAmount,
    int? TargetChunkCount,
    int MinChunkChars,
    int MaxOverrunChars)
{
    /// <summary>The embedding model's own token ceiling, less the library's safety margin.</summary>
    public const int DefaultTokenLimit = 256;

    /// <summary>Context sentences kept on each side while measuring — the library's recommended start.</summary>
    public const int DefaultBufferSize = 1;

    /// <summary>The library's default breakpoint strategy and its documented industry default.</summary>
    public const string DefaultThresholdType = "Percentile";

    /// <summary>The 95th distance percentile, which is the default for the default strategy.</summary>
    public const double DefaultThresholdAmount = 95;

    /// <summary>Drops chunks that hold nothing but whitespace.</summary>
    public const int DefaultMinChunkChars = 1;

    /// <summary>How far past the limit the splitter looks for a newline before cutting hard.</summary>
    public const int DefaultMaxOverrunChars = 200;
}
