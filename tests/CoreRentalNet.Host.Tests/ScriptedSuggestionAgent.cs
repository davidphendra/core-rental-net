using System.Runtime.CompilerServices;
using CoreRentalNet.Host.Agents;

namespace CoreRentalNet.Host.Tests;

/// <summary>A hand-written agent that replays a scripted run.</summary>
/// <remarks>
/// The port's shape is the fake's shape, so this is the same thing the real adapter is: a sequence of events.
/// It replays exactly the fragments it was given rather than chunking them itself, because where a fragment
/// boundary falls is the whole subject of the streaming tests - a fake that chose its own boundaries would be
/// testing itself.
/// </remarks>
/// <param name="events">The events to replay, in order.</param>
/// <param name="then">What the transport does when the script runs out - a stop, most of the time.</param>
internal sealed class ScriptedSuggestionAgent(AgentSuggestionEvent[] events, Exception? then = null)
    : ISuggestionAgent
{
    public async IAsyncEnumerable<AgentSuggestionEvent> StreamAsync(
        SuggestionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();

        foreach (var raised in events)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return raised;
        }

        if (then is not null)
        {
            throw then;
        }
    }
}
