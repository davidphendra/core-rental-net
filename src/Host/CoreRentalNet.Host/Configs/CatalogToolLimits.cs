namespace CoreRentalNet.Host.Configs;

/// <summary>What one MCP tool answer will carry, and what one MCP tool will accept.</summary>
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
    /// <summary>The rows a tool answer carries when the caller names no limit.</summary>
    public const int DefaultProductCount = 12;

    /// <summary>The most rows a tool answer will carry, whatever the caller asks for.</summary>
    public const int MaximumProductCount = 25;

    /// <summary>The longest description a catalogue tool publishes to a model.</summary>
    /// <remarks>
    /// A tool answer is model context, not a catalogue page. The REST answer keeps the full description; only
    /// the MCP boundary publishes this bounded form. The rule lives beside the other tool-boundary bounds so a
    /// reader finds every "what a tool may carry" number in one place.
    /// </remarks>
    public const int MaximumDescriptionCharacterCount = 150;

    /// <summary>The longest one search term may be.</summary>
    public const int MaximumSearchTermCharacterCount = 80;

    /// <summary>The most terms one call will search with.</summary>
    /// <remarks>
    /// <b>The same number the agent's vocabulary-limit policy bounds one component to, and it cannot be stated
    /// once.</b> The policy belongs to the agent's solution and this to the application's, so each side pins the
    /// number with its own test and the contract is what keeps them equal.
    /// <para>
    /// It is eight because recall is already saturated well below it: measured over the real catalogue, three
    /// terms within one category return the whole category, so a ninth buys almost nothing while it dilutes the
    /// ranking the cap is spent on. The answer window is twelve; this input bound remains eight.
    /// </para>
    /// </remarks>
    public const int MaximumSearchTermCount = 8;

    /// <summary>The longest a single typed search text may be.</summary>
    public const int MaximumSearchTextCharacterCount = 400;

    /// <summary>The requested cap, bounded so a caller cannot ask for the whole catalogue.</summary>
    public static int Clamp(int limit)
        => limit < 1 ? DefaultProductCount : Math.Min(limit, MaximumProductCount);
}
