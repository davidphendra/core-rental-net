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
/// The SKUs are real catalogue SKUs, and a test asserts that: a stand-in holding a SKU the catalogue does
/// not would exercise the drop path in every scenario and quietly make the happy path untested.
/// </para>
/// </remarks>
internal static class ScenarioLibrary
{
    // Desk, chair and monitor, at the three price levels the picker chooses between.
    private const string Essential = """
        {"kind":"stage","stage":"verifying","attempt":1}
        {"kind":"stage","stage":"rephrasing","attempt":1}
        {"kind":"stage","stage":"selecting","attempt":1}
        {"kind":"stage","stage":"reviewing","attempt":1}
        {"kind":"result","status":"ok","options":[{"tier":"essential","lines":[{"slot":"desk","sku":"DSKWWZEB3USL","quantity":1},{"slot":"chair","sku":"CHAE2V0VGJZ8","quantity":1},{"slot":"monitor","sku":"MONAA5DU36L3","quantity":1}],"criteria":["desk","chair","monitor"],"unevaluated":[],"pinnedSlots":[]},{"tier":"balanced","lines":[{"slot":"desk","sku":"DSK72C2U9DMR","quantity":1},{"slot":"chair","sku":"CHA9COSVF201","quantity":1},{"slot":"monitor","sku":"MONWBHJHV4BD","quantity":1}],"criteria":["desk","chair","monitor"],"unevaluated":[],"pinnedSlots":[]},{"tier":"premium","lines":[{"slot":"desk","sku":"DSKQXUN06SC1","quantity":1},{"slot":"chair","sku":"CHA3ELOX8PP8","quantity":1},{"slot":"monitor","sku":"MON3Q5UEGB63","quantity":1}],"criteria":["desk","chair","monitor"],"unevaluated":[],"pinnedSlots":[]}]}
        """;

    /// <summary>The run with an objection it could not settle.</summary>
    private const string Exhausted = """
        {"kind":"stage","stage":"verifying","attempt":1}
        {"kind":"stage","stage":"rephrasing","attempt":1}
        {"kind":"stage","stage":"selecting","attempt":1}
        {"kind":"stage","stage":"reviewing","attempt":1}
        {"kind":"stage","stage":"selecting","attempt":2}
        {"kind":"stage","stage":"reviewing","attempt":2}
        {"kind":"result","status":"exhausted","code":"budget","options":[],"findings":["budget"]}
        """;

    /// <summary>A request that is not about a workspace, and is declined.</summary>
    private const string Rejected = """
        {"kind":"stage","stage":"verifying","attempt":1}
        {"kind":"result","status":"rejected","code":"not-a-workspace-request","options":[],"findings":["not-a-workspace-request"]}
        """;

    /// <summary>A request nothing in the catalogue satisfies.</summary>
    private const string Unavailable = """
        {"kind":"stage","stage":"verifying","attempt":1}
        {"kind":"stage","stage":"rephrasing","attempt":1}
        {"kind":"stage","stage":"selecting","attempt":1}
        {"kind":"result","status":"unavailable","code":"nothing-satisfies","options":[],"findings":["nothing-satisfies"]}
        """;

    /// <summary>
    /// A run that says something the contract does not allow: a line that is not JSON, a stage the
    /// application has no words for, and an unparseable result.
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
        {"kind":"result","status":"ok","options":[{"tier":"essential","lines":[{"slot":"desk","sku":"DSKWWZEB3USL","quantity":1}],"criteria":[],"unevaluated":[],"pinnedSlots":[]}]}
        """;

    public static string For(string name) => name switch
    {
        "exhausted" => Exhausted,
        "rejected" => Rejected,
        "unavailable" => Unavailable,
        "malformed" => Malformed,
        _ => Essential,
    };

    /// <summary>Every scenario's name, for the endpoint that lists them and the tests that drive them.</summary>
    public static IReadOnlyList<string> Names => ["essential", "exhausted", "rejected", "unavailable", "malformed"];

    /// <summary>Every SKU any scenario names, so a test can hold them against the catalogue.</summary>
    public static IReadOnlyList<string> Skus
        => [.. Names.SelectMany(name => SkusOf(For(name)))];

    private static IEnumerable<string> SkusOf(string body)
        => body.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .SelectMany(line => System.Text.RegularExpressions.Regex.Matches(line, "\"sku\":\"([^\"]+)\""))
            .Select(match => match.Groups[1].Value);
}
