namespace CoreRentalNet.Host.Agents;

/// <summary>One thing that happened during a run, in order.</summary>
/// <remarks>
/// A closed hierarchy: the three things a caller can be told are the three types below it, so a
/// <c>switch</c> over them is exhaustive and a fourth kind cannot appear without a compile error at every
/// call site. The application's streamed endpoint is the only consumer.
/// </remarks>
internal abstract record AgentSuggestionEvent
{
    private AgentSuggestionEvent()
    {
    }

    /// <summary>A fragment of the agent's own text, exactly as it arrived.</summary>
    internal sealed record NarrativeDelta(string Text) : AgentSuggestionEvent;

    /// <summary>The terminal answer, with the hash of the payload it was drawn from.</summary>
    internal sealed record Completed(AgentSuggestionResult Result, string PayloadHash) : AgentSuggestionEvent;

    /// <summary>The run could not be made: no agent, no identity, no transport, no parse, or too slow.</summary>
    internal sealed record Unavailable(string Reason) : AgentSuggestionEvent;
}
