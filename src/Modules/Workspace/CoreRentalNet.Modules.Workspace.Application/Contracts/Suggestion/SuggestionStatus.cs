namespace CoreRentalNet.Modules.Workspace.Application.Contracts.Suggestion;

/// <summary>The four ways a suggestion request can end, named once.</summary>
/// <remarks>
/// Three come from the agent and are results - it either suggested or classified. The fourth is the
/// application's own and means the agent never answered: a transport failure is not a refusal, and a
/// page that confused the two would tell a customer their request was wrong when the service was down.
/// </remarks>
public static class SuggestionStatus
{
    /// <summary>The reviewer approved the candidates.</summary>
    public const string Ok = "ok";

    /// <summary>The attempts ran out and the candidates travel with the findings against them.</summary>
    public const string Exhausted = "exhausted";

    /// <summary>The request was judged not to be about a workspace.</summary>
    public const string Rejected = "rejected";

    /// <summary>The application could not reach the agent. Its own outcome, never the agent's.</summary>
    public const string Unavailable = "unavailable";
}
