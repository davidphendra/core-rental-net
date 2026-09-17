namespace CoreRentalNet.Host.Agents;

/// <summary>
/// The hosted agent's answer as text, one message per update, exactly as the platform streams it.
/// </summary>
/// <remarks>
/// A port rather than the framework's <c>AIAgent</c> directly, so that what the adapter does with the
/// text - the part that can be wrong - is testable without a client, a credential or a network. The
/// implementation that drives <c>AIAgent</c> is a handful of lines and is verified against a real
/// endpoint; this shape is what the parsing is written against.
/// </remarks>
internal interface IAgentTextStream
{
    IAsyncEnumerable<string> AskAsync(string query, CancellationToken cancellationToken = default);
}
