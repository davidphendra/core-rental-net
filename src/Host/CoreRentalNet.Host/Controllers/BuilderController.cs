using CoreRentalNet.Host.Configs;
using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Host.Presentation.WorkspaceSuggestion;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;
using CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreRentalNet.Host.Controllers;

/// <summary>The endpoint a suggestion run is started from: the whole run, from the bound body to the last frame.</summary>
/// <remarks>
/// <para>
/// A controller, and this route used to be the one exception to that. Its answer is held open and written to
/// for as long as the run takes, which is not the shape an action result is written for - but an action that
/// writes to the response and returns no result is how MVC expresses the same thing, so the endpoint is
/// declared where every other one is: the route is an attribute, the body is bound by the framework, the gate
/// is an attribute, and the answers are on the operation.
/// </para>
/// <para>
/// <b>Internal, and that is not an oversight to tidy up.</b> A run needs the agent adapter, and that adapter
/// names provider types that are internal to the module. A public type may not name it, so this controller is
/// internal and <see cref="InternalControllerFeatureProvider"/> is what lets MVC discover it.
/// </para>
/// <para>
/// <b>The permission is declared here, and that is not the same thing as hiding the panel that calls it.</b> A
/// customer without the entitlement is never shown the suggestion panel, but a panel that is not rendered is
/// presentation rather than authorisation: anyone can post to a path they have not been shown. This endpoint is
/// where a run is paid for, so this endpoint is where the permission is checked.
/// </para>
/// <para>
/// <b>The endpoint composes, relays and delegates; it interprets nothing.</b> The request is built by the
/// module's factory, and the run - its stages, its frames, its ending and its record - is the module's run
/// service. What is left here is the transport: read the token, build the payload, open the stream, write each
/// frame the run produces, and let the response end.
/// </para>
/// </remarks>
[ApiController]
[Route(BuilderRoutes.Suggest)]
internal sealed class BuilderController(
    IWorkspaceSuggestionRequestFactory requestPayloadFactory,
    IWorkspaceSuggestionRunService suggestionRunService,
    IAccessTokenService accessTokenService,
    ILoggerFactory loggerFactory) : ControllerBase
{
    /// <summary>The refusal shape every problem on this API arrives as, named rather than left to the formatters.</summary>
    private const string ProblemJson = "application/problem+json";

    private readonly ILogger _logger = loggerFactory.CreateLogger<BuilderController>();

    /// <summary>Runs one suggestion for this customer, and streams what happens while it runs.</summary>
    /// <remarks>
    /// <para>
    /// <b>Nothing reaches this method that is not worth running.</b> A body that carries no sentence is refused
    /// by <c>[ApiController]</c> from the requirement declared on <see cref="WorkspaceSuggestionQueryRequest"/>,
    /// before the action runs and before anything is spent. A run costs roughly 172k input tokens, which is why
    /// the requirement is on the bound type rather than trusted to the caller.
    /// </para>
    /// <para>
    /// <b>The action writes its answer rather than returning one, and that is what a run needs.</b> The response
    /// is held open and written to for as long as the agent takes, in frames the browser reads as they arrive;
    /// an action that returns no result is how MVC expresses that. Opening the stream commits the headers, so a
    /// failure from that point on cannot be answered with a status code - which is why the payload is built, the
    /// permission checked and the token read before it.
    /// </para>
    /// <para>
    /// <b>The run is a sequence, and this is where it reaches the wire.</b> The module yields the frames a run
    /// produces and names no transport at all; this action opens the stream and writes each frame as it arrives,
    /// so a frame the customer is reading is a frame that has already been flushed. A cancellation - the customer
    /// stopping, or the agent's own call giving up - is an ending rather than an error: it is not logged, and the
    /// stream ends cleanly, because a response that aborts mid-stream reads to the browser as a broken run rather
    /// than a stopped one.
    /// </para>
    /// <para>
    /// <b>The customer's own token is read fresh, and refreshed when it has expired, through the
    /// caller-token service</b> - which reads the identity SDK, the only place expiry is decided - then placed in
    /// the run's own scope rather than passed along; the catalogue answers for the token itself when the agent
    /// presents it, and only the agent can tell "no token" from "a token the catalogue refused".
    /// </para>
    /// </remarks>
    [HttpPost]
    [Authorize(Policy = WorkspaceSuggestionPolicy.Name)]
    [Produces(ServerSentWorkspaceSuggestionEventWriter.MediaType)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, ProblemJson)]
    public async Task Suggest([FromBody] WorkspaceSuggestionQueryRequest suggestionQueryRequest)
    {
        var previousRunContext = RunScopeAccessToken.Current;
        var cancellationToken = HttpContext.RequestAborted;

        try
        {
            if (await ReadTheRunContextAsync() is not { } runContext)
            {
                return;                              // refused before the stream opened, as a status code
            }

            RunScopeAccessToken.Current = runContext;

            var suggestionRequestPayload = requestPayloadFactory.Create(
                new WorkspaceSuggestionQuery(
                    suggestionQueryRequest.Query!,
                    suggestionQueryRequest.CeilingMonthly
                )
            );

            var suggestionEventWriter = new ServerSentWorkspaceSuggestionEventWriter(Response);

            await suggestionEventWriter.BeginAsync(cancellationToken);

            await foreach (var streamEvent in suggestionRunService.StreamAsync(
                                                suggestionRequestPayload,
                                                User.HashCustomerIdentity(),
                                                cancellationToken))
            {
                await suggestionEventWriter.WriteAsync(streamEvent, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // The customer stopped, or the run did: the stream ends, and the run's record says how it ended.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when running workspace suggestion");
        }
        finally
        {
            RunScopeAccessToken.Current = previousRunContext;
        }
    }

    /// <summary>Reads the caller's own token for the run, and refuses the run when the session can produce none.</summary>
    /// <remarks>
    /// <para>
    /// <b>The context is returned rather than placed in the scope here, and that is load-bearing.</b> An
    /// <c>AsyncLocal</c> is written into the execution context of whichever flow does the writing, and a write
    /// made after an <c>await</c> inside a called async method does not travel back out to its caller - so the
    /// endpoint has to open the run's scope in its own flow, or the pipeline reads nothing at all.
    /// </para>
    /// <para>
    /// <b>The refusal is written here rather than returned, because this action returns no result</b> - and it
    /// happens before the stream opens, because a status code is only still possible while the headers are
    /// uncommitted.
    /// </para>
    /// </remarks>
    private async Task<RunContext?> ReadTheRunContextAsync()
    {
        string accessToken;

        try
        {
            accessToken = await accessTokenService.GetAsync();
        }
        catch (CallerAccessTokenUnavailableException)
        {
            await RefuseBecauseTheSessionCannotProduceATokenAsync();

            return null;
        }

        return new RunContext(accessToken);
    }

    /// <summary>Refuses a run the session can no longer produce a catalogue token for.</summary>
    /// <remarks>
    /// <para>
    /// <b>Written rather than returned, because this action returns no result.</b> It holds the response open for
    /// the run, and a result that is never executed sets nothing — so <c>Problem(...)</c> here would leave the
    /// response a success and hand the customer an empty stream instead of a refusal.
    /// </para>
    /// <para>
    /// <b>401, not 403.</b> The caller is still signed in to the application; it is the session that can no longer
    /// produce a token for the catalogue - the identity SDK found the access token expired and had no refresh to
    /// replace it with. That is a re-login, and the panel words it that way.
    /// </para>
    /// </remarks>
    private async Task RefuseBecauseTheSessionCannotProduceATokenAsync()
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.ContentType = ProblemJson;

        await Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Your session has expired. Sign in again to keep building.",
            },
            options: null,
            contentType: ProblemJson,
            HttpContext.RequestAborted);
    }
}
