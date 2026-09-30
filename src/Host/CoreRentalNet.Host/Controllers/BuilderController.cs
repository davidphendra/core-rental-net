using CoreRentalNet.Host.Configs;
using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Host.Presentation.WorkspaceSuggestion;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;
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
/// <b>The endpoint composes and delegates; it interprets nothing.</b> The request is built by the module's
/// factory, and the run - its stages, its stream, its ending and its record - is the module's run service. What
/// is left here is the transport: read the token, build the payload, open the stream, hand over.
/// </para>
/// </remarks>
[ApiController]
[Route(BuilderRoutes.Suggest)]
internal sealed class BuilderController(
    IWorkspaceSuggestionRequestFactory suggestionRequestFactory,
    IWorkspaceSuggestionRunService suggestionRunService) : ControllerBase
{
    /// <summary>The refusal shape every problem on this API arrives as, named rather than left to the formatters.</summary>
    private const string ProblemJson = "application/problem+json";

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
    /// failure from that point on cannot be answered with a status code - which is why the payload is built and
    /// the permission checked before it.
    /// </para>
    /// <para>
    /// <b>The customer's own token is forwarded, not checked.</b> The gate above answered whether this caller may
    /// run at all; the catalogue answers for the token itself when the agent presents it, and only the agent can
    /// tell "no token" from "a token the catalogue refused". The agent takes it out of the model's input before
    /// the run, so what reaches the prompt is the sentence and the slot rules and nothing else.
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
        // Read from the sign-in session, on the one request that has one, and forwarded rather than checked: the
        // catalogue answers for the token itself when the agent presents it.
        var callerAccessToken = await HttpContext.GetAccessTokenAsync().ConfigureAwait(false);

        // Refused before the stream opens and before a run is paid for. A run without the caller's token cannot
        // read the catalogue at all, so this is a misconfiguration being reported as one rather than a run being
        // charged for and quietly failing at the far end.
        if (string.IsNullOrWhiteSpace(callerAccessToken))
        {
            await RefuseBecauseTheSignInCarriedNoTokenAsync();

            return;
        }

        // Built before the stream opens, so a payload that cannot be built is still a status code.
        var suggestionRequestPayload = suggestionRequestFactory.Create(
            new WorkspaceSuggestionQuery(suggestionQueryRequest.Query!, suggestionQueryRequest.CeilingMonthly));

        var suggestionEventWriter = await ServerSentWorkspaceSuggestionEventWriter.BeginAsync(
            Response,
            HttpContext.RequestAborted);

        await suggestionRunService.RunSuggestionAsync(
            suggestionRequestPayload,
            callerAccessToken,
            suggestionEventWriter,
            User.HashCustomerIdentity(),
            HttpContext.RequestAborted);
    }

    /// <summary>Refuses a run the caller's sign-in gave no token for, as a status code and a problem body.</summary>
    /// <remarks>
    /// <b>Written rather than returned, because this action returns no result.</b> It holds the response open for
    /// the run, and a result that is never executed sets nothing — so <c>Problem(...)</c> here would leave the
    /// response a success and hand the customer an empty stream instead of a refusal.
    /// </remarks>
    private async Task RefuseBecauseTheSignInCarriedNoTokenAsync()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        Response.ContentType = ProblemJson;

        await Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Detail = "This sign-in carried no token for the application's API, so a workspace suggestion " +
                         "cannot be run.",
            },
            options: null,
            contentType: ProblemJson,
            HttpContext.RequestAborted);
    }
}
