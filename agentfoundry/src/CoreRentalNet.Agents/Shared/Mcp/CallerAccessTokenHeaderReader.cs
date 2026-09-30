using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CoreRentalNet.Agents.Shared.Mcp;

/// <summary>Reads the caller's catalogue token off the invocation the run arrived in.</summary>
/// <remarks>
/// <para>
/// <b>The token is out of band from the request body.</b> The body is what the hosting layer records and what a
/// model may be handed, and a bearer token belongs in neither — so this is how the run is given one without any
/// stage having to take it back out of a message.
/// </para>
/// <para>
/// <b>Only the presence is ever logged, never the value.</b> A token in a log line is a token on its way out of
/// the trust boundary, and log lines travel further than request bodies do.
/// </para>
/// </remarks>
internal sealed class CallerAccessTokenHeaderReader(
    IHttpContextAccessor httpContextAccessor,
    ILogger<CallerAccessTokenHeaderReader> logger)
{
    /// <summary>The token this invocation carries, or null when it carried none.</summary>
    public string? ReadTheCallersCatalogueAccessToken()
    {
        var carriedValue = httpContextAccessor.HttpContext?.Request.Headers[CallerAccessTokenHeader.Name];

        var callerAccessToken = carriedValue is { Count: > 0 } ? carriedValue.ToString() : null;

        logger.LogInformation(
            "Caller catalogue token {Presence} on the invocation.",
            string.IsNullOrEmpty(callerAccessToken) ? "absent" : "present");

        return callerAccessToken;
    }
}
