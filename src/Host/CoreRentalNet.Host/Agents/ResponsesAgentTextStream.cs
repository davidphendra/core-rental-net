using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;

namespace CoreRentalNet.Host.Agents;

/// <summary>
/// Drives an <c>AIAgent</c> and yields what it streams, as text.
/// </summary>
/// <remarks>
/// The whole of the framework's involvement on the client side, kept to one class so that everything
/// that can be wrong about reading the agent - the parsing, the delimiters, the missing result - is in
/// <see cref="FoundryAgentAdapter"/> and testable without any of this.
/// </remarks>
internal sealed class ResponsesAgentTextStream(AIAgent agent) : IAgentTextStream
{
    public async IAsyncEnumerable<string> AskAsync(
        string query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // No session is passed, and that is deliberate: one request is one run with no memory of any
        // other. A session here would give the agent a conversation nobody designed for.
        await foreach (var update in agent.RunStreamingAsync(query, cancellationToken: cancellationToken))
        {
            if (update.Text is { Length: > 0 } text)
            {
                yield return text;
            }
        }
    }
}
