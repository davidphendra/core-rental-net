using System.Diagnostics;
using System.Text;
using System.Text.Json;
using CoreRentalNet.Host.Agents;
using Microsoft.Extensions.Logging;

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
    SuggestionValidator validator,
    SuggestionEventStream stream,
    ILoggerFactory loggers)
{
    private readonly NarrativeFieldReader _reader = new();

    /// <summary>Everything the model wrote, kept for the record: the raw answer is what a bad run is debugged on.</summary>
    private readonly StringBuilder _raw = new();

    private string _payloadHash = string.Empty;
    private string _query = string.Empty;
    private AgentRunUsage? _usage;
    private AiRunVerdict _verdict = AiRunVerdict.Unavailable;

    /// <summary>Writes the whole run, and returns when the stream has ended.</summary>
    /// <param name="customer">
    /// The account this run is for, already made opaque. The record names a run by a hash rather than by an
    /// account, and the caller is the one holding the token service that does it.
    /// </param>
    public async Task RunAsync(RunRequest ask, string customer, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();

        _query = ask.Query?.Trim() ?? string.Empty;

        try
        {
            await WriteAsync(ask, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Stopped, and worth distinguishing from a failure in the record: one is a customer who changed
            // their mind and the other is something to fix.
            _verdict = AiRunVerdict.Stopped;

            throw;
        }
        finally
        {
            // Written however the run ended, including when it was stopped: a run with no record is exactly the
            // run nobody can explain.
            AiRunLog.Ran(loggers.CreateLogger(AiRunLog.Category), Record(customer, started));
        }
    }

    /// <summary>What this run was, assembled once it is over and there is nothing left to wait for.</summary>
    private AiRunRecord Record(string customer, long started)
        => new(
            RunId: Guid.NewGuid().ToString("n"),
            Query: _query,
            PayloadHash: _payloadHash,
            Model: _usage?.Model ?? string.Empty,
            PromptVersion: _usage?.PromptVersion ?? string.Empty,
            ModelCalls: _usage?.ModelCalls ?? 0,
            InputTokens: _usage?.InputTokens ?? 0,
            OutputTokens: _usage?.OutputTokens ?? 0,
            LatencyMilliseconds: (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds,
            RawOutput: _raw.ToString(),
            Verdict: _verdict,
            CustomerId: customer);

    /// <summary>Writes the whole run, and returns when the stream has ended.</summary>
    private async Task WriteAsync(RunRequest ask, CancellationToken cancellationToken)
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
                _raw.Append(delta.Text);

                await WriteFieldsAsync(delta.Text, cancellationToken);
                return false;

            case AgentSuggestionEvent.Completed completed:
                return await CompleteAsync(completed, cancellationToken);

            case AgentSuggestionEvent.Unavailable:
                _verdict = AiRunVerdict.Unavailable;

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

    /// <summary>The answer: checked before any of it is shown, and the verdict the record keeps.</summary>
    private async Task<bool> CompleteAsync(
        AgentSuggestionEvent.Completed completed,
        CancellationToken cancellationToken)
    {
        await stream.StageAsync(SuggestionStageCopy.Checking, cancellationToken);

        // The run's cost arrives beside the answer rather than inside it: a model cannot observe how many calls
        // it took, and asking it produced a plausible invention that validation accepted.
        _usage = completed.Result.RunUsage;
        _payloadHash = completed.PayloadHash;

        if (!Frames(completed.Result, out var frame))
        {
            _verdict = AiRunVerdict.Invalid;

            await stream.FailedAsync(SuggestionEventStream.Invalid, cancellationToken);

            return true;
        }

        _verdict = frame.Status is SuggestionResultFrame.NotWorkspace
            ? AiRunVerdict.Refused
            : AiRunVerdict.Suggested;

        await stream.ResultAsync(
            JsonSerializer.Serialize(frame, SuggestionJson.Options),
            cancellationToken);

        return true;
    }

    /// <summary>
    /// The frame the browser is given, or nothing when the answer cannot be honoured.
    /// </summary>
    /// <remarks>
    /// <b>Nothing is rendered from an unchecked answer.</b> A refusal is a result and travels as one; a
    /// suggestion is only a suggestion once every line of it has been checked and priced from the catalogue.
    /// The whole answer is checked before any of it is shown, so a candidate is never half-valid.
    /// </remarks>
    private bool Frames(AgentSuggestionResult result, out SuggestionResultFrame frame)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Status is AgentSuggestionStatus.NotWorkspace)
        {
            frame = SuggestionResultFrame.Refused();

            return true;
        }

        if (!validator.TryValidate(result, out var candidates))
        {
            frame = null!;

            return false;
        }

        frame = SuggestionResultFrame.Of(candidates);

        return true;
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
