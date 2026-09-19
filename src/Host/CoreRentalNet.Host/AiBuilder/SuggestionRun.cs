using System.Text.Json;
using CoreRentalNet.Host.Agents;

namespace CoreRentalNet.Host.AiBuilder;

/// <summary>
/// One suggestion run: what the customer is told while it happens, and how it ends.
/// </summary>
/// <remarks>
/// <para>
/// The two halves of a run are kept apart on purpose. The <b>stages</b> are the application's own words,
/// written at the moment each phase actually happens — the sentence read, the catalogue matched, the answer
/// checked — and they are not the agent's: the agent's stage identities never reach this application at all
/// (see the story's resolved risk), so there is no vocabulary to translate.
/// </para>
/// <para>
/// The <b>model's words</b> pass through <see cref="NarrativeFieldReader"/>, which emits a field only once its
/// JSON string has closed, and <see cref="OutputHygiene"/>, which runs on that complete value. Raw JSON is
/// never written, and neither is a partial value — a URL split across two streamed fragments cannot escape a
/// strip that never runs on half a URL.
/// </para>
/// <para>
/// The <b>result</b> is written whole or not at all: one frame, after the reader has seen the end of the
/// answer. Nothing derived from it is rendered before validation has had it (`e05s08`).
/// </para>
/// </remarks>
internal sealed class SuggestionRun(
    ISuggestionAgent agent,
    SuggestionRequestBuilder requests,
    SuggestionEventStream stream)
{
    private readonly NarrativeFieldReader _reader = new();

    /// <summary>Writes the whole run, and returns when the stream has ended.</summary>
    public async Task RunAsync(RunRequest ask, CancellationToken cancellationToken)
    {
        await stream.StageAsync(SuggestionStageCopy.Reading, cancellationToken);

        // Read here rather than inside the agent: what crosses the wire is the query and this application's
        // own projection, and the projection is the phase the second stage names.
        var request = requests.Build(ask);

        await stream.StageAsync(SuggestionStageCopy.Matching, cancellationToken);

        await foreach (var raised in agent.StreamAsync(request, cancellationToken))
        {
            if (await HandleAsync(raised, cancellationToken))
            {
                return;
            }
        }
    }

    /// <summary>Writes one event, and says whether the run is over.</summary>
    private async Task<bool> HandleAsync(AgentSuggestionEvent raised, CancellationToken cancellationToken)
    {
        switch (raised)
        {
            case AgentSuggestionEvent.NarrativeDelta delta:
                await WriteFieldsAsync(delta.Text, cancellationToken);
                return false;

            case AgentSuggestionEvent.Completed completed:
                await stream.StageAsync(SuggestionStageCopy.Checking, cancellationToken);
                await stream.ResultAsync(
                    JsonSerializer.Serialize(completed.Result, SuggestionJson.Options),
                    cancellationToken);
                return true;

            case AgentSuggestionEvent.Unavailable:
                // The reason is diagnostic, not customer-facing: the browser words this outcome, and the
                // reason itself belongs on the run record rather than on the page.
                await stream.FailedAsync(SuggestionEventStream.Unavailable, cancellationToken);
                return true;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(raised),
                    raised,
                    "No run outcome is written for this event.");
        }
    }

    /// <summary>Writes the narrative fields that have closed since the previous fragment, hygiened first.</summary>
    private async Task WriteFieldsAsync(string fragment, CancellationToken cancellationToken)
    {
        foreach (var field in _reader.Feed(fragment))
        {
            await stream.TextAsync(OutputHygiene.Apply(field.Kind, field.Text), cancellationToken);
        }
    }
}
