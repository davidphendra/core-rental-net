using CoreRentalNet.Host.Agents;
using CoreRentalNet.Host.AiBuilder;

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
/// <param name="stop">Signalled once the script has been replayed, so a test can model a customer who stops
/// <b>after</b> reading something. Without it, a stopped run would be one that never started: a token cancelled
/// before the first write makes every later write throw, which is a different story from the one the tests tell.
/// </param>
internal sealed class ScriptedSuggestionAgent(
    AgentSuggestionEvent[] events,
    Exception? then = null,
    CancellationTokenSource? stop = null) : ISuggestionAgent
{
    public async IAsyncEnumerable<AgentSuggestionEvent> StreamAsync(
        SuggestionRequest request,
        RunBudget budget)
    {
        await Task.Yield();

        // The run's token is deliberately NOT checked here. This fake replays what it was given and then does
        // what `then` says, and the real adapter's cancellation behaviour is proved against the real adapter -
        // in StandInTransportTests and SuggestionAgentTests. A fake that also policed the token would make the
        // stopped-run tests model the cancellation twice: once by the token and once by the script.
        foreach (var raised in events)
        {
            yield return raised;
        }

        stop?.Cancel();

        if (then is not null)
        {
            throw then;
        }
    }
}
