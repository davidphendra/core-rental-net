namespace CoreRentalNet.Host.AiBuilder;

/// <summary>
/// The words the application puts around a run, and the only ones it says while one is going.
/// </summary>
/// <remarks>
/// <para>
/// A run is a paid model call and a wait of the better part of a minute, so the customer is told what is
/// happening - in this application's words rather than in the agent's. These two lines describe the run's own
/// stages: the request being read, and the catalogue the agent searches for itself. None of them is a thing the
/// agent can name, which is why the agent is not asked for it.
/// </para>
/// <para>
/// <b>There is no third stage, and there used to be one that said the answer was being checked.</b> The
/// application no longer checks the answer against the catalogue - the agent states the names and the amounts
/// from the tool results it was given, and the workspace is what refuses a composition that cannot be honoured
/// when one is applied. A stage saying otherwise would be the one thing these lines must not be, which is a
/// lie to a customer who is waiting.
/// </para>
/// </remarks>
internal static class SuggestionStageCopy
{
    /// <summary>The sentence each stage is announced with, in the order the stages happen.</summary>
    /// <remarks>
    /// A property rather than a stored collection: the order is the meaning, and nothing that can be mutated at
    /// runtime should be able to change what a customer is told.
    /// </remarks>
    public static IReadOnlyList<string> Sequence => [Reading, Matching];

    /// <summary>The customer's sentence is being read and turned into a specification.</summary>
    public const string Reading = "Reading your request";

    /// <summary>The catalogue the agent searches is being matched against that specification.</summary>
    public const string Matching = "Matching the catalogue";
}
