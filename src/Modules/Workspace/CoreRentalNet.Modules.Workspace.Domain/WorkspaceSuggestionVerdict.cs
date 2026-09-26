namespace CoreRentalNet.Modules.Workspace.Domain;

/// <summary>How a suggestion run ended, in the one word a record needs.</summary>
/// <remarks>
/// The distinction that matters is between an answer the application could use and the three ways there was
/// none: "the agent refused" and "the agent was unreachable" want different fixes, and a stopped run wants no
/// fix at all. It lives in the module's vocabulary rather than on the record because it is what the run was.
/// </remarks>
public enum WorkspaceSuggestionVerdict
{
    /// <summary>Candidates came back and are being shown.</summary>
    Suggested,

    /// <summary>The typed verdict that the request was not about a workspace. A result, not a failure.</summary>
    Refused,

    /// <summary>The answer said it suggested something and carried no candidate at all.</summary>
    Invalid,

    /// <summary>No agent, no identity, or no transport.</summary>
    Unavailable,

    /// <summary>The customer stopped it, or went away. Not a failure.</summary>
    Stopped,
}
