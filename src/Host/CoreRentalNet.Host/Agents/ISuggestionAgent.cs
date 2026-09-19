namespace CoreRentalNet.Host.Agents;

/// <summary>The application's only knowledge of the agent.</summary>
/// <remarks>
/// Its shape is the fake's shape. An <b>unavailable</b> agent and a <b>refusal</b> are different outcomes and
/// arrive as different events: the first is a failure the customer retries, the second is an answer the
/// application words.
/// </remarks>
internal interface ISuggestionAgent
{
    /// <summary>Runs one suggestion, yielding what happens as it happens.</summary>
    IAsyncEnumerable<AgentSuggestionEvent> StreamAsync(SuggestionRequest request, CancellationToken cancellationToken);
}
