namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>The words the application puts around a run, and the only ones it says while one is going.</summary>
/// <remarks>
/// A run is a paid model call and a wait of the better part of a minute, so the customer is told what is
/// happening - in this application's words rather than in the agent's. None of these is a thing the agent can
/// name, which is why the agent is not asked for it.
/// </remarks>
public static class WorkspaceSuggestionStage
{
    /// <summary>The sentence each stage is announced with, in the order the stages happen.</summary>
    public static IReadOnlyList<string> Sequence => [Reading, Matching];

    /// <summary>The customer's sentence is being read and turned into a specification.</summary>
    public const string Reading = "Reading your request";

    /// <summary>The catalogue the agent searches is being matched against that specification.</summary>
    public const string Matching = "Matching the catalogue";
}
