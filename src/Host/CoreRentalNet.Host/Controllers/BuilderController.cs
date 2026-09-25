using System.Diagnostics;
using System.Text.Json;
using CoreRentalNet.Host.Agents;
using CoreRentalNet.Host.AiBuilder;
using CoreRentalNet.Host.Configs;
using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Host.Presentation;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Rules;
using CoreRentalNet.Modules.Workspace.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CoreRentalNet.Host.Controllers;

/// <summary>
/// The endpoint a suggestion run is started from: the whole run, from the bound body to the last frame.
/// </summary>
/// <remarks>
/// <para>
/// A controller, and this route used to be the one exception to that. Its answer is held open and written to
/// for as long as the run takes, which is not the shape an action result is written for - but an action that
/// writes to the response and returns no result is how MVC expresses the same thing, so the endpoint is
/// declared where every other one is: the route is an attribute, the body is bound by the framework, the gate
/// is an attribute, and the answers are on the operation. The streaming itself is unchanged.
/// </para>
/// <para>
/// <b>Internal, and that is not an oversight to tidy up.</b> A run needs the agent, and the agent is internal to
/// this application on purpose. A public type may not name it, and widening it would reach the whole
/// agent-facing graph, so this controller is internal and <see cref="InternalControllerFeatureProvider"/> is what
/// lets MVC discover it. Making this type public would be a compile error rather than a tidy-up, which is the
/// point.
/// </para>
/// <para>
/// <b>The permission is declared here, and that is not the same thing as hiding the section that calls it.</b>
/// A customer without the entitlement is never shown the AI section, but a section that is not rendered is
/// presentation rather than authorisation: anyone can post to a path they have not been shown. This endpoint
/// is where a run is paid for, so this endpoint is where the permission is checked - a caller who never sees
/// the section must not be able to spend anything either.
/// </para>
/// <para>
/// <b>The run is one request's worth of state held in fields, and that is safe because a controller instance is
/// created per request.</b> The reader and the ledger used to be a separate object built inside the action; they
/// are the same two values either way, and nothing is shared between two runs.
/// </para>
/// <para>
/// <b>The catalogue is a dependency because a run states the currency it thinks in, and for nothing else.</b> The
/// whole payload a run sends is built here, and it is the sentence, the slot rules and that currency - never a
/// product. The agent searches the catalogue for itself, through the MCP tools this application publishes.
/// </para>
/// </remarks>
[ApiController]
[Route(BuilderRoutes.Suggest)]
internal sealed class BuilderController(
    ISuggestionAgent agent,
    IProductCatalogService everyProduct,
    WorkspaceSlotSettings slots,
    ILoggerFactory loggers) : ControllerBase
{
    /// <summary>The refusal shape every problem on this API arrives as, named rather than left to the formatters.</summary>
    private const string ProblemJson = "application/problem+json";

    /// <summary>Splits the agent's streamed text into the fields a customer may be shown.</summary>
    private readonly NarrativeFieldReader _reader = new();

    /// <summary>What this run accumulates: what was asked, what it cost, and how it ended.</summary>
    private readonly RunLedger _ledger = new();

    /// <summary>Runs one suggestion for this customer, and streams what happens while it runs.</summary>
    /// <remarks>
    /// <para>
    /// <b>Nothing reaches this method that is not worth running.</b> A body that carries no sentence is refused
    /// by <c>[ApiController]</c> from the requirement declared on <see cref="WorkspaceQueryRequest"/>, before the
    /// action runs and before anything is spent - the same way the catalogue's endpoint refuses a filter that is
    /// not one of its words. A run costs roughly 172k input tokens, which is why the requirement is on the
    /// bound type rather than trusted to the caller.
    /// </para>
    /// <para>
    /// <b>The action writes its answer rather than returning one, and that is what a run needs.</b> The response
    /// is held open and written to for as long as the agent takes, in frames the browser reads as they arrive;
    /// an action that returns no result is how MVC expresses that. Opening the stream commits the headers, so a
    /// failure from that point on cannot be answered with a status code - which is why nothing below it is
    /// allowed to throw past the cancellation it expects.
    /// </para>
    /// <para>
    /// <b>The customer's own token is forwarded, not checked.</b> The gate above answered whether this caller may
    /// run at all; the catalogue answers for the token itself when the agent presents it, and only the agent can
    /// tell "no token" from "a token the catalogue refused". The agent takes it out of the model's input before
    /// the run, so what reaches the prompt is the sentence and the slot rules and nothing else.
    /// </para>
    /// <para>
    /// <b>The only way this application learns that a customer stopped is that their connection went away.</b>
    /// Stop aborts the stream from the browser, and reloading or navigating away drops it - the same signal, so
    /// this is one rule rather than a second concurrency semantic. When it happens there is nobody left to write
    /// to, so nothing is written and nothing is reported as an error: the run was not a failure, and the customer
    /// who stopped is the party that knows it, so the page is where the words belong. Whatever was already
    /// streamed stands, because nothing retracts it mid-read.
    /// </para>
    /// <para>
    /// <b>A cancellation that is not the customer's is recorded and nothing else.</b> Nothing in this application
    /// raises one, because the run has no deadline - the agent adapter turns every other failure into an event -
    /// so it is the transport giving up, which is a failure rather than an ending. Nothing is written for it,
    /// because a token that is not cancelled is no promise that the response is still connected.
    /// </para>
    /// <para>
    /// <b>The record is written whichever way the run ended</b>, including when it was stopped, because a run with
    /// no record is exactly the run nobody can explain. The customer is named by a hash of what this application
    /// already identifies them by, so a reader can see that two runs belong to the same account without the
    /// record holding an account.
    /// </para>
    /// </remarks>
    [HttpPost]
    [Authorize(Policy = AiPolicy.Name)]
    [Produces(SuggestionEventStream.MediaType)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, ProblemJson)]
    public async Task Suggest([FromBody] WorkspaceQueryRequest workspaceQueryRequest)
    {
        // Forwarded rather than checked, and named for the record by a hash: see the remarks.
        var accessToken = await HttpContext.GetAccessTokenAsync().ConfigureAwait(false) ?? string.Empty;
        var started = Stopwatch.GetTimestamp();

        _ledger.Query = workspaceQueryRequest.Query!.Trim();

        try
        {
            var stream = await SuggestionEventStream.BeginAsync(Response, HttpContext.RequestAborted);

            await stream.StageAsync(SuggestionStageCopy.Reading, HttpContext.RequestAborted);
            await stream.StageAsync(SuggestionStageCopy.Matching, HttpContext.RequestAborted);

            var request = Build(workspaceQueryRequest, accessToken);

            await foreach (var raised in agent.StreamAsync(request, HttpContext.RequestAborted))
            {
                if (await HandleAsync(stream, raised, HttpContext.RequestAborted))
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // One cancellation, two meanings, decided by whose token was signalled.
            _ledger.Verdict = HttpContext.RequestAborted.IsCancellationRequested
                ? AiRunVerdict.Stopped
                : AiRunVerdict.Unavailable;
        }
        finally
        {
            AiRunLog.Ran(loggers.CreateLogger(AiRunLog.Category), _ledger.For(CustomerKey.HashedOf(User), started));
        }
    }

    /// <summary>Builds the one payload a run sends: the customer's sentence and the slot rules.</summary>
    /// <remarks>
    /// <para>
    /// <b>The catalogue is not pushed.</b> A run used to carry either the whole compact projection (~336 KB ≈
    /// 86k tokens, twice, against a deployment that allows 100,000 tokens a minute) or a shortlist retrieved by
    /// this application. Both are gone: the agent reaches the catalogue through the MCP tools the Host publishes,
    /// so this sends the sentence and the rules and nothing else. The catalogue is read here for exactly one
    /// value - the currency the request is stated in.
    /// </para>
    /// <para>
    /// <b>Only the query, the rules and the currency cross to Foundry.</b> No identity, no address, no draft, no
    /// session, no SKU. The slot rules are sent because the model must not propose a composition the application
    /// would refuse; the capacities are configuration, read once at start-up. The token is the caller's, taken
    /// from the sign-in session, and it is left out of the payload hash because the hash identifies what the run
    /// was drawn from rather than who asked.
    /// </para>
    /// </remarks>
    private SuggestionRequest Build(WorkspaceQueryRequest workspaceQueryRequest, string accessToken)
        => new(
            RunId: Guid.NewGuid().ToString("n"),
            Query: workspaceQueryRequest.Query!.Trim(),
            Currency: CompactCatalogProjection.CurrencyOf(everyProduct.All),
            CeilingMonthly: workspaceQueryRequest.CeilingMonthly,
            Slots: EverySlot(),
            McpAccessToken: accessToken);

    /// <summary>Every slot the domain has, with the capacity configuration gives it.</summary>
    /// <remarks>
    /// Every slot rather than the slots a draft is using: the agent composes a whole workspace, and the rule it
    /// is told is the rule the application will check it against. Taken from the enum, so a slot added to the
    /// domain is one the agent is told about rather than one it silently ignores.
    /// </remarks>
    private IReadOnlyList<SuggestionSlotRule> EverySlot()
        => [.. Enum.GetValues<SlotId>().Select(slot => new SuggestionSlotRule(slot, slots.CapacityFor(slot)))];

    /// <summary>Writes one event, and says whether the run is over.</summary>
    private async Task<bool> HandleAsync(
        SuggestionEventStream stream,
        AgentSuggestionEvent raised,
        CancellationToken cancellationToken)
    {
        switch (raised)
        {
            case AgentSuggestionEvent.NarrativeDelta delta:
                _ledger.Raw.Append(delta.Text);

                // Every complete field, hygiened, as the model finishes it: raw JSON is never written, and
                // neither is a partial value.
                foreach (var field in _reader.Feed(delta.Text))
                {
                    await stream.TextAsync(OutputHygiene.Apply(field.Kind, field.Text), cancellationToken);
                }

                return false;

            case AgentSuggestionEvent.Completed completed:
                return await CompleteAsync(stream, completed, cancellationToken);

            case AgentSuggestionEvent.Unavailable:
                _ledger.Verdict = AiRunVerdict.Unavailable;

                // The reason is diagnostic, not customer-facing: the browser words this outcome, and the reason
                // itself belongs on the run record rather than on the page.
                await stream.FailedAsync(SuggestionEventStream.Unavailable, cancellationToken);
                return true;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(raised),
                    raised,
                    "No run outcome is written for this event.");
        }
    }

    /// <summary>The answer: framed for the browser, and the verdict the record keeps.</summary>
    private async Task<bool> CompleteAsync(
        SuggestionEventStream stream,
        AgentSuggestionEvent.Completed completed,
        CancellationToken cancellationToken)
    {
        // The run's cost arrives beside the answer rather than inside it: a model cannot observe how many calls
        // it took, and asking it produced a plausible invention that validation accepted.
        _ledger.Usage = completed.Result.RunUsage;
        _ledger.PayloadHash = completed.PayloadHash;

        var frame = SuggestionResultFrame.From(completed.Result);

        if (frame is null)
        {
            _ledger.Verdict = AiRunVerdict.Invalid;

            await stream.FailedAsync(SuggestionEventStream.Invalid, cancellationToken);

            return true;
        }

        _ledger.Verdict = frame.Status is SuggestionResultFrame.NotWorkspace
            ? AiRunVerdict.Refused
            : AiRunVerdict.Suggested;

        // Whole or not at all: one frame, after the reader has seen the end of the answer, so nothing derived
        // from it is written before the model's last field has closed.
        await stream.ResultAsync(
            JsonSerializer.Serialize(frame, SuggestionJson.Options),
            cancellationToken);

        return true;
    }
}
