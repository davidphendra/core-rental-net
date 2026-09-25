namespace CoreRentalNet.Host.AiBuilder;

/// <summary>How a run ended, in the one word a record needs.</summary>
/// <remarks>
/// The distinction that matters is between an answer the application could use and the three ways there was
/// none - because "the agent refused" and "the agent was unreachable" want different fixes, and a stopped run
/// wants no fix at all.
/// </remarks>
internal enum AiRunVerdict
{
    /// <summary>Candidates came back and are being shown.</summary>
    Suggested,

    /// <summary>The typed verdict that the request was not about a workspace. A result, not a failure.</summary>
    Refused,

    /// <summary>The answer said it suggested something and carried no option at all.</summary>
    Invalid,

    /// <summary>No agent, no identity, or no transport.</summary>
    Unavailable,

    /// <summary>The customer stopped it, or went away. Not a failure.</summary>
    Stopped,
}
