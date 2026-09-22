namespace CoreRentalNet.Host.Configs;

/// <summary>What one MCP tool answer will carry, and how much of a sentence a tool will accept.</summary>
/// <remarks>
/// <para>
/// <b>Smaller than <see cref="CatalogApiLimits.Default"/> on purpose.</b> A tool answer is spent on a model's
/// context rather than shown to a page, so returning the catalogue through a tool would defeat the reason the
/// agent searches at all. The envelope still reports how many matched, so a capped answer is never mistaken
/// for a complete one.
/// </para>
/// <para>
/// The input bounds are here rather than beside the search because they are all "what the tool boundary will
/// accept": one idea, one file.
/// </para>
/// </remarks>
internal static class CatalogToolLimits
{
    /// <summary>The rows a tool answer carries when the model names no limit.</summary>
    public const int Default = 8;

    /// <summary>The most a tool answer will carry, whatever the model asks for.</summary>
    public const int Max = 25;

    /// <summary>The longest term or sentence a tool will send to a search.</summary>
    public const int MaxQueryLength = 400;

    /// <summary>The requested cap, bounded so a caller cannot ask for the whole catalogue.</summary>
    public static int Clamp(int limit) => limit < 1 ? Default : Math.Min(limit, Max);
}
