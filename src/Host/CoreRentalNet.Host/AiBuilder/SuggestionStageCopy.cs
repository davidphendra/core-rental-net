namespace CoreRentalNet.Host.AiBuilder;

/// <summary>
/// The words the application puts around a run, and the only ones it says while one is going.
/// </summary>
/// <remarks>
/// <para>
/// A run is a paid model call and a wait that reaches 45 seconds, so the customer is told what is
/// happening - in this application's words rather than in the agent's. These three lines describe the
/// run's own stages: the request being read, the catalogue being matched against it, and the answer
/// being checked. None of them is a thing the agent can name, which is why the agent is not asked for it.
/// </para>
/// <para>
/// The agent's own stage identities are mapped onto this vocabulary rather than forwarded, so a stage
/// renamed inside the agent changes nothing a customer reads. The mapping arrives with the run itself;
/// this is the app-owned sequence, and it is deliberately the whole of what a run says before the model
/// has said anything.
/// </para>
/// </remarks>
internal static class SuggestionStageCopy
{
    /// <summary>The sentence each stage is announced with, in the order the stages happen.</summary>
    /// <remarks>
    /// A property rather than a stored collection: the order is the meaning, and nothing that can be
    /// mutated at runtime should be able to change what a customer is told.
    /// </remarks>
    public static IReadOnlyList<string> Sequence => [Reading, Matching, Checking];

    /// <summary>The customer's sentence is being read and turned into a specification.</summary>
    public const string Reading = "Reading your request";

    /// <summary>The catalogue this application holds is being matched against that specification.</summary>
    public const string Matching = "Matching the catalogue";

    /// <summary>What came back is being checked before the customer is shown any of it.</summary>
    public const string Checking = "Checking the suggestion";
}
