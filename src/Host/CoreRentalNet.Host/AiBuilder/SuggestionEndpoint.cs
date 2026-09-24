using CoreRentalNet.Host.Agents;
using CoreRentalNet.Host.Configs;
using CoreRentalNet.Host.Infrastructure;
using Microsoft.Extensions.Logging;
using CoreRentalNet.BuildingBlocks.Application;
using System.Security.Claims;

namespace CoreRentalNet.Host.AiBuilder;

/// <summary>
/// The endpoint a suggestion run is started from, and the two things in front of it: the permission that
/// says who may spend, and the guard that says how many may spend at once.
/// </summary>
/// <remarks>
/// <para>
/// A minimal-API endpoint rather than a controller. The answer is held open and written to for as long as
/// the run takes, which is not the shape an MVC action is written for, even though the catalogue's
/// endpoint beside it is a controller.
/// </para>
/// <para>
/// <b>The permission is declared here, and that is not the same thing as hiding the section that calls
/// it.</b> A customer without the entitlement is never shown the AI section, but a section that is not
/// rendered is presentation rather than authorisation: anyone can post to a path they have not been
/// shown. This endpoint is where a run is paid for, so this endpoint is where the permission is checked -
/// a caller who never sees the section must not be able to spend anything either.
/// </para>
/// </remarks>
internal static class SuggestionEndpoint
{
    /// <summary>Maps the run endpoint. Called from the pipeline beside the controllers.</summary>
    public static void MapSuggestionEndpoint(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost(BuilderRoutes.Suggest, StreamAsync)
            .RequireAuthorization(AiPolicy.Name);
    }

    /// <summary>Runs one suggestion for this customer, or refuses before anything is spent.</summary>
    private static async Task StreamAsync(
        HttpContext context,
        RunRequest ask,
        RunGuard guard,
        ISuggestionAgent agent,
        SuggestionAgentSettings settings,
        SuggestionRequestBuilder requests,
        SuggestionValidator validator,
        IOpaqueTokenService tokens,
        ILoggerFactory loggers)
    {
        if (string.IsNullOrWhiteSpace(ask.Query))
        {
            Refuse(context, StatusCodes.Status400BadRequest, ApiErrorCode.InvalidRequest);
            return;
        }

        var lease = guard.TryBegin(context.User);

        if (lease is null)
        {
            Refuse(context, StatusCodes.Status409Conflict, ApiErrorCode.RunInFlight);
            return;
        }

        using (lease)
        {
            await WriteAsync(context, ask, agent, settings, requests, validator, tokens, loggers);
        }
    }

    /// <summary>Writes one run, and treats the customer's cancellation as an ordinary end.</summary>
    /// <remarks>
    /// <b>The only way this application learns that a customer stopped is that their connection went away.</b>
    /// Stop aborts the stream from the browser, and reloading or navigating away drops it - the same signal,
    /// so this is one rule rather than a second concurrency semantic. When it happens there is nobody left to
    /// write to, so nothing is written and nothing is reported as an error: the run was not a failure, and the
    /// customer who stopped is the party that knows it, so the page is where the words belong. Whatever was
    /// already streamed stands, because nothing retracts it mid-read.
    /// </remarks>
    private static async Task WriteAsync(
        HttpContext context,
        RunRequest ask,
        ISuggestionAgent agent,
        SuggestionAgentSettings settings,
        SuggestionRequestBuilder requests,
        SuggestionValidator validator,
        IOpaqueTokenService tokens,
        ILoggerFactory loggers)
    {
        try
        {
            var stream = await SuggestionEventStream.BeginAsync(context.Response, context.RequestAborted);

            // The record names the customer by a hash of what this application already identifies them by, so a
            // reader can see that two runs belong to the same account without the record holding an account.
            var customer = tokens.HashOf(CustomerKey.Of(context.User) ?? "anonymous");

            // The run's deadline opens HERE, before the agent is asked. It used to open inside the agent, which
            // left everything the application does around the call outside the number the run is allowed.
            using var budget = RunBudget.Over(context.RequestAborted, settings.TimeoutSeconds);

            await new SuggestionRun(agent, requests, validator, stream, loggers)
                .RunAsync(ask, customer, budget);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Stopped. Nothing to write, nothing to report, and the lease above is released by the caller's
            // `using` whichever way this returns - which is the whole of what a stopped run has to do.
        }
    }

    /// <summary>
    /// Refuses a request without streaming anything.
    /// </summary>
    /// <remarks>
    /// The status code is left as the whole answer: the problem-details service writes the body, so a
    /// refusal here carries the same members as every other refusal on this API, and the code names the
    /// reason rather than the status. Both refusals happen before the response is committed, which is why
    /// they can be status codes at all.
    /// </remarks>
    private static void Refuse(HttpContext context, int status, string code)
    {
        ApiErrorCode.Set(context, code);
        context.Response.StatusCode = status;
    }
}
