using System.Diagnostics;
using System.Text.Json;
using CoreRentalNet.Host.Agents;
using CoreRentalNet.Modules.Discovery.Application.Selection;
using CoreRentalNet.Modules.Discovery.Application.Shortlist;
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
    ICatalogShortlist shortlist,
    IOfferSelection offers,
    RetrievalFacts retrieval,
    SuggestionValidator validator,
    SuggestionEventStream stream,
    ILoggerFactory loggers)
{    private readonly NarrativeFieldReader _reader = new();

    /// <summary>The catalogue search and its two failure endings, kept out of this class's own budget.</summary>
    private readonly CatalogSearch _search = new(shortlist, stream);

    /// <summary>What the run offers the model, recorded once.</summary>
    private readonly OfferRecorder _offers = new(offers, loggers);

    /// <summary>Everything the run accumulates: what was asked, what it was given, what it cost, how it ended.</summary>
    private readonly RunLedger _ledger = new();

    /// <summary>Writes the whole run, and returns when the stream has ended.</summary>
    /// <param name="customer">
    /// The account this run is for, already made opaque. The record names a run by a hash rather than by an
    /// account, and the caller is the one holding the token service that does it.
    /// </param>
    public async Task RunAsync(RunRequest ask, string customer, RunBudget budget)
    {
        ArgumentNullException.ThrowIfNull(budget);

        var started = Stopwatch.GetTimestamp();

        _ledger.Query = ask.Query?.Trim() ?? string.Empty;

        try
        {
            await WriteAsync(ask, budget);
        }
        catch (OperationCanceledException) when (budget.Customer.IsCancellationRequested)
        {
            // Stopped, and worth distinguishing from a failure in the record: one is a customer who changed
            // their mind and the other is something to fix.
            _ledger.Verdict = AiRunVerdict.Stopped;

            throw;
        }
        catch (OperationCanceledException)
        {
            // THE RUN'S OWN DEADLINE, AND IT IS A FAILURE RATHER THAN AN ENDING. Until e06 this could not
            // happen here: the budget opened inside the agent, which turned its own expiry into an unavailable
            // event before it ever reached this method. Now that the budget covers retrieval too, an expiry
            // during the search arrives here as a cancellation - and reporting it as "stopped" would put a
            // customer's decision in the record for something the application did to itself.
            _ledger.Verdict = AiRunVerdict.Unavailable;

            await _search.TimedOutAsync(budget);
        }
        finally
        {
            // Written however the run ended, including when it was stopped: a run with no record is exactly the
            // run nobody can explain.
            AiRunLog.Ran(loggers.CreateLogger(AiRunLog.Category), _ledger.For(customer, started, retrieval));
        }
    }


    /// <summary>Writes the whole run, and returns when the stream has ended.</summary>
    private async Task WriteAsync(RunRequest ask, RunBudget budget)
    {
        await stream.StageAsync(SuggestionStageCopy.Reading, budget.Run);

        // The catalogue is searched BEFORE the model is asked, and the whole point of the epic is that what the
        // model is then shown is the result of that search rather than all 205 products.
        var products = await _search.ProductsAsync(_ledger.Query, budget);

        if (products is null)
        {
            // A failure the customer retries. The verdict already means "no agent, no identity, no transport, or
            // too slow"; this adds a cause to it rather than an outcome.
            _ledger.Verdict = AiRunVerdict.Unavailable;
            return;
        }

        _ledger.Shortlist = [.. products.Select(item => item.Sku)];

        await stream.StageAsync(SuggestionStageCopy.Matching, budget.Run);

        // Read here rather than inside the agent: what crosses the wire is the query and this application's
        // own projection, and the projection is the phase the second stage names.
        var request = requests.Build(ask, products);

        await foreach (var raised in agent.StreamAsync(request, budget))
        {
            // Not every event means the model was reached: the adapter yields UNAVAILABLE when it cannot build
            // the agent, when the transport refuses, or when a run times out - and none of those is an offer.
            // What proves it is an event only a model can produce: a narrative fragment, or an answer.
            if (raised is not AgentSuggestionEvent.Unavailable)
            {
                await _offers.RecordAsync(_ledger.Shortlist, budget);
            }

            if (await HandleAsync(raised, budget.Run))
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
                _ledger.Raw.Append(delta.Text);

                await WriteFieldsAsync(delta.Text, cancellationToken);
                return false;

            case AgentSuggestionEvent.Completed completed:
                return await CompleteAsync(completed, cancellationToken);

            case AgentSuggestionEvent.Unavailable:
                _ledger.Verdict = AiRunVerdict.Unavailable;

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
        _ledger.Usage = completed.Result.RunUsage;
        _ledger.PayloadHash = completed.PayloadHash;

        if (!Frames(completed.Result, out var frame))
        {
            _ledger.Verdict = AiRunVerdict.Invalid;

            await stream.FailedAsync(SuggestionEventStream.Invalid, cancellationToken);

            return true;
        }

        _ledger.Verdict = frame.Status is SuggestionResultFrame.NotWorkspace
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
