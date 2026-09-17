using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;

namespace CoreRentalNet.Host.Agents;

/// <summary>
/// Drives an <c>AIAgent</c> and yields what it streams, as text.
/// </summary>
/// <remarks>
/// <para>
/// The whole of the framework's involvement on the client side, kept to one class so that everything
/// that can be wrong about reading the agent - the parsing, the delimiters, the missing result - is in
/// <see cref="FoundryAgentAdapter"/> and testable without any of this.
/// </para>
/// <para>
/// It is also the translator. The SDK reports a refused connection, a rejected call and a broken stream
/// as its own exception types, and the application layer must not know them: everything that is not a
/// cancellation becomes the one exception it already understands, so "the agent could not be reached"
/// has a single meaning on the inside however many ways the client can find to say it.
/// </para>
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
        var updates = ReadAsync(query, cancellationToken).GetAsyncEnumerator(cancellationToken);

        await using (updates.ConfigureAwait(false))
        {
            while (true)
            {
                var (moved, text) = await NextAsync(updates).ConfigureAwait(false);

                if (!moved)
                {
                    yield break;
                }

                yield return text!;
            }
        }
    }

    /// <summary>
    /// The next piece of the agent's answer, or nothing when it has finished.
    /// </summary>
    /// <remarks>
    /// One move in one method, so that the translation below sits outside the loop that yields: a
    /// <c>yield return</c> cannot live inside a try with a catch, and the alternative - catching in the
    /// caller - would leave the failure of the first move and the failure of the tenth told differently.
    /// </remarks>
    private static async Task<(bool Moved, string? Text)> NextAsync(IAsyncEnumerator<string> updates)
    {
        try
        {
            return await updates.MoveNextAsync().ConfigureAwait(false)
                ? (true, updates.Current)
                : (false, null);
        }
        catch (Exception failure) when (failure is not OperationCanceledException)
        {
            // A cancellation is the customer's, and passes through as itself.
            throw new HttpRequestException($"the agent could not be reached: {failure.Message}", failure);
        }
    }

    private async IAsyncEnumerable<string> ReadAsync(
        string query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var update in agent
            .RunStreamingAsync(query, cancellationToken: cancellationToken)
            .ConfigureAwait(false))
        {
            if (update.Text is { Length: > 0 } text)
            {
                yield return text;
            }
        }
    }
}
