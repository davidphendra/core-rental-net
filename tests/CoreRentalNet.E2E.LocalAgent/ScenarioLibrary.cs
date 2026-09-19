namespace CoreRentalNet.E2E.LocalAgent;

/// <summary>What the stand-in answers with, written as the agent's own contract.</summary>
/// <remarks>
/// <para>
/// Each body is the agent's answer: the rephraser's specification first, the suggestor's result last, one
/// JSON object per line — the two-object sequence a sequential workflow produces, which is the shape the
/// application's reader was built for and the one its most expensive lesson is about.
/// </para>
/// <para>
/// <b>Purpose only.</b> Every <c>rationale</c> and every line's <c>why</c> is a phrase about the work, never
/// a price and never a product name, because those are the fields the customer reads as they arrive. The
/// prompts require it; this fixture has to be an example of it rather than an exception to it.
/// </para>
/// <para>
/// <b>Real SKUs, and a test holds them there.</b> A stand-in naming a SKU the catalogue does not have would
/// exercise the drop path in every scenario and leave the happy path untested nowhere - which is the kind of
/// quiet failure a fixture makes possible.
/// </para>
/// <para>
/// <b>The three options genuinely spread.</b> The dearest is about twice the cheapest by catalogue price, so
/// the 1.5x rule the application enforces after it recomputes the amounts is satisfied by the fixture rather
/// than by luck. A fixture whose options clustered would fail validation in every scenario and prove nothing
/// about the ones that pass.
/// </para>
/// </remarks>
internal static class ScenarioLibrary
{
    /// <summary>The happy path: a specification, then three candidates that spread.</summary>
    private const string Suggested = """
        {"status":"spec","reason":null,"ceilingMonthly":null,"slots":[{"slot":"Desk","quantity":1,"purpose":"a wide, stable surface"},{"slot":"Chair","quantity":1,"purpose":"support for long sessions"},{"slot":"Monitor","quantity":1,"purpose":"a display at eye level"}],"constraints":["a small room"]}
        {"status":"suggested","reason":null,"options":[{"lines":[{"slot":"Desk","sku":"DSKB08XN4JDR","quantity":1,"why":"a wide, stable surface"},{"slot":"Chair","sku":"CHAB091PDL8V","quantity":1,"why":"support for long sessions"},{"slot":"Monitor","sku":"MONB001AO2QL","quantity":1,"why":"a display at eye level"}],"rationale":"An uncluttered setup for one person, kept inside a small room."},{"lines":[{"slot":"Desk","sku":"DSKB0B6MCK87","quantity":1,"why":"a wide, stable surface"},{"slot":"Chair","sku":"CHAB09R9WB61","quantity":1,"why":"support for long sessions"},{"slot":"Monitor","sku":"MONB000PB2KW","quantity":1,"why":"a display at eye level"}],"rationale":"The same arrangement with a sturdier chair and a larger screen, for someone who sits for hours."},{"lines":[{"slot":"Desk","sku":"DSKB07PFFFQ2","quantity":1,"why":"a wide, stable surface"},{"slot":"Chair","sku":"CHAB0BYXJBVW","quantity":1,"why":"support for long sessions"},{"slot":"Monitor","sku":"MONB00112PRM","quantity":1,"why":"a display at eye level"}],"rationale":"A heavier desk and a taller chair, for a corner that will not be rearranged."}]}
        """;

    /// <summary>The same answer, delivered a piece at a time.</summary>
    /// <remarks>
    /// A run that moved faster than the browser could be read would turn the assertions that matter into
    /// races rather than checks: a stage list with stages in it, streamed text with text in it, and a
    /// cancellation with something left to interrupt.
    /// </remarks>
    private const string Slow = Suggested;

    /// <summary>A request that is not about furnishing a workspace, declined rather than failed.</summary>
    private const string Refused = """
        {"status":"notWorkspace","reason":"not about furnishing a workspace"}
        {"status":"notWorkspace","reason":"not about furnishing a workspace","options":[]}
        """;

    public static string For(string name) => name switch
    {
        "refused" => Refused,
        "slow" => Slow,
        "broken" => Suggested,
        _ => Suggested,
    };

    /// <summary>How long the stand-in waits between events, in milliseconds.</summary>
    public static int DelayMilliseconds(string name) => name == "slow" ? 600 : 0;

    /// <summary>True when the scenario is one where nothing answers at all.</summary>
    /// <remarks>
    /// The only way to reach the application's own outcome: an agent that refuses and an agent that
    /// exhausts itself are both answering.
    /// </remarks>
    public static bool IsBroken(string name) => name == "broken";

    /// <summary>Every scenario's name, for the endpoint that lists them and the tests that drive them.</summary>
    public static IReadOnlyList<string> Names => ["suggested", "refused", "slow", "broken"];

    /// <summary>Every SKU any scenario names, so a test can hold them against the catalogue.</summary>
    public static IReadOnlyList<string> Skus
        => [.. Names.SelectMany(name => SkuPattern.Matches(For(name))).Select(match => match.Groups[1].Value).Distinct()];

    private static readonly System.Text.RegularExpressions.Regex SkuPattern =
        new("\"sku\":\"([^\"]+)\"");
}
