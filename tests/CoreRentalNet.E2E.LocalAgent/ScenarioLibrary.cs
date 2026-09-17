namespace CoreRentalNet.E2E.LocalAgent;

/// <summary>
/// What the stand-in says, written as the agent's own contract.
/// </summary>
/// <remarks>
/// <para>
/// The bodies are newline-delimited JSON in the shape the agent publishes - one stage per line, then one
/// result - and they are deliberately the agent's words rather than a paraphrase: a stand-in that
/// invented its own shape would prove the application reads something, not that it reads this.
/// </para>
/// <para>
/// Everything here is inside the contract's closed vocabularies, and a test holds it there. That is not
/// ceremony: the first version of this file offered tiers named "essential" and "premium" and a status of
/// "unavailable", and the schema allows none of them. Every test passed, because the application
/// faithfully rendered what it was handed - so a fixture outside the contract tests the application
/// against a language it will never hear.
/// </para>
/// <para>
/// The SKUs are real catalogue SKUs, and a test asserts that: a stand-in holding a SKU the catalogue does
/// not would exercise the drop path in every scenario and quietly make the happy path untested.
/// </para>
/// </remarks>
internal static class ScenarioLibrary
{
    /// <summary>The happy path: four stages, then three tiers of three lines.</summary>
    private const string Essential = """
        {"kind":"stage","stage":"verifying","attempt":1}
        {"kind":"stage","stage":"rephrasing","attempt":1}
        {"kind":"stage","stage":"selecting","attempt":1}
        {"kind":"stage","stage":"reviewing","attempt":1}
        {"kind":"result","status":"ok","options":[{"tier":"low","lines":[{"slot":"Desk","sku":"DSKWWZEB3USL","quantity":1},{"slot":"Chair","sku":"CHAE2V0VGJZ8","quantity":1},{"slot":"Monitor","sku":"MONAA5DU36L3","quantity":1}],"criteria":["slot:desk", "slot:chair", "slot:monitor"],"unevaluated":[],"pinnedSlots":[]},{"tier":"middle","lines":[{"slot":"Desk","sku":"DSK72C2U9DMR","quantity":1},{"slot":"Chair","sku":"CHA9COSVF201","quantity":1},{"slot":"Monitor","sku":"MONWBHJHV4BD","quantity":1}],"criteria":["slot:desk", "slot:chair", "slot:monitor"],"unevaluated":[],"pinnedSlots":[]},{"tier":"high","lines":[{"slot":"Desk","sku":"DSKQXUN06SC1","quantity":1},{"slot":"Chair","sku":"CHA3ELOX8PP8","quantity":1},{"slot":"Monitor","sku":"MON3Q5UEGB63","quantity":1}],"criteria":["slot:desk", "slot:chair", "slot:monitor"],"unevaluated":[],"pinnedSlots":[]}]}
""";

    /// <summary>A word the catalogue cannot express, carried back as the customer wrote it.</summary>
    private const string Unmet = """
        {"kind":"stage","stage":"verifying","attempt":1}
        {"kind":"stage","stage":"rephrasing","attempt":1}
        {"kind":"stage","stage":"selecting","attempt":1}
        {"kind":"stage","stage":"reviewing","attempt":1}
        {"kind":"result","status":"ok","options":[{"tier":"low","lines":[{"slot":"Desk","sku":"DSKWWZEB3USL","quantity":1},{"slot":"Chair","sku":"CHAE2V0VGJZ8","quantity":1},{"slot":"Monitor","sku":"MONAA5DU36L3","quantity":1}],"criteria":["slot:desk", "slot:chair"],"unevaluated":[{"phrase":"barefoot","reason":"not_in_catalogue"}],"pinnedSlots":[]},{"tier":"middle","lines":[{"slot":"Desk","sku":"DSK72C2U9DMR","quantity":1},{"slot":"Chair","sku":"CHA9COSVF201","quantity":1},{"slot":"Monitor","sku":"MONWBHJHV4BD","quantity":1}],"criteria":["slot:desk", "slot:chair"],"unevaluated":[{"phrase":"barefoot","reason":"not_in_catalogue"}],"pinnedSlots":[]},{"tier":"high","lines":[{"slot":"Desk","sku":"DSKQXUN06SC1","quantity":1},{"slot":"Chair","sku":"CHA3ELOX8PP8","quantity":1},{"slot":"Monitor","sku":"MON3Q5UEGB63","quantity":1}],"criteria":["slot:desk", "slot:chair"],"unevaluated":[{"phrase":"barefoot","reason":"not_in_catalogue"}],"pinnedSlots":[]}]}
""";

    /// <summary>A run that spent its attempts and still had an objection.</summary>
    private const string Exhausted = """
        {"kind":"stage","stage":"verifying","attempt":1}
        {"kind":"stage","stage":"rephrasing","attempt":1}
        {"kind":"stage","stage":"selecting","attempt":1}
        {"kind":"stage","stage":"reviewing","attempt":1}
        {"kind":"stage","stage":"selecting","attempt":2}
        {"kind":"stage","stage":"reviewing","attempt":2}
        {"kind":"result","status":"exhausted","options":[{"tier":"low","lines":[{"slot":"Desk","sku":"DSKWWZEB3USL","quantity":1},{"slot":"Chair","sku":"CHAE2V0VGJZ8","quantity":1},{"slot":"Monitor","sku":"MONAA5DU36L3","quantity":1}],"criteria":["slot:desk", "slot:chair", "slot:monitor"],"unevaluated":[],"pinnedSlots":[]},{"tier":"middle","lines":[{"slot":"Desk","sku":"DSK72C2U9DMR","quantity":1},{"slot":"Chair","sku":"CHA9COSVF201","quantity":1},{"slot":"Monitor","sku":"MONWBHJHV4BD","quantity":1}],"criteria":["slot:desk", "slot:chair", "slot:monitor"],"unevaluated":[],"pinnedSlots":[]},{"tier":"high","lines":[{"slot":"Desk","sku":"DSKQXUN06SC1","quantity":1},{"slot":"Chair","sku":"CHA3ELOX8PP8","quantity":1},{"slot":"Monitor","sku":"MON3Q5UEGB63","quantity":1}],"criteria":["slot:desk", "slot:chair", "slot:monitor"],"unevaluated":[],"pinnedSlots":[]}],"findings":[{"kind":"criteria_not_met","slot":"Chair"}]}
""";

    /// <summary>A request that is not about a workspace, declined rather than failed.</summary>
    private const string Rejected = """
        {"kind":"stage","stage":"verifying","attempt":1}
        {"kind":"result","status":"rejected","code":"not_workspace_request","options":[]}
""";

    /// <summary>
    /// A run that says something the contract does not allow: a line that is not JSON, and a stage the
    /// application has no words for.
    /// </summary>
    /// <remarks>
    /// Kept as a scenario because the adapter's promise is that a message it cannot read costs a line of
    /// progress and nothing more. A fixture that only ever sent well-formed messages would leave that
    /// promise untested on the wire.
    /// </remarks>
    private const string Malformed = """
        {"kind":"stage","stage":"verifying","attempt":1}
        not json at all
        {"kind":"stage","stage":"a-stage-nobody-defined","attempt":1}
        {"kind":"result","status":"ok","options":[{"tier":"low","lines":[{"slot":"Desk","sku":"DSKWWZEB3USL","quantity":1}],"criteria":["slot:desk"],"unevaluated":[],"pinnedSlots":[]}]}
""";

    public static string For(string name) => name switch
    {
        "unmet" => Unmet,
        "exhausted" => Exhausted,
        "rejected" => Rejected,
        "malformed" => Malformed,
        // The same answer as the happy path, delivered slowly. A run is up to ten model calls across
        // three attempts, so a fixture that only ever answered instantly would leave the two things that
        // matter about a long run untestable: a cancel with something to interrupt, and a stage list
        // with time to be read.
        "slow" => Essential,
        // Nothing answers at all, which is the only way to reach the outcome the application reaches by
        // itself: an agent that refuses and an agent that exhausts itself are both answering.
        "broken" => Essential,
        _ => Essential,
    };

    /// <summary>How long the stand-in waits between events, in milliseconds.</summary>
    /// <remarks>
    /// Long enough that a watching test has time to observe each state: a run that moved faster than the
    /// browser could be read would make the assertions that matter - a stage list with stages in it, a
    /// cancel with something to interrupt - into races rather than checks.
    /// </remarks>
    public static int DelayMilliseconds(string name) => name == "slow" ? 900 : 0;

    /// <summary>True when the scenario is one where nothing answers.</summary>
    public static bool IsBroken(string name) => name == "broken";

    /// <summary>Every scenario's name, for the endpoint that lists them and the tests that drive them.</summary>
    public static IReadOnlyList<string> Names
        => ["essential", "unmet", "slow", "exhausted", "rejected", "malformed", "broken"];

    /// <summary>Every scenario by name, so a test can hold each body against the contract.</summary>
    public static IReadOnlyList<(string Name, string Body)> All
        => [.. Names.Select(name => (name, For(name)))];

    /// <summary>Every SKU any scenario names, so a test can hold them against the catalogue.</summary>
    public static IReadOnlyList<string> Skus => [.. All.SelectMany(scenario => SkusOf(scenario.Body))];

    private static IEnumerable<string> SkusOf(string body)
        => body.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .SelectMany(line => SkuPattern.Matches(line))
            .Select(match => match.Groups[1].Value);

    private static readonly System.Text.RegularExpressions.Regex SkuPattern = new("\"sku\":\"([^\"]+)\"");
}
