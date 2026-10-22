using System.Diagnostics;
using System.Net.Http.Headers;
using CoreRentalNet.Agents.Shared.Telemetry;

namespace CoreRentalNet.Agents.Shared.Mcp;

/// <summary>Attaches the call's own catalogue token, and only to a request the policy approved.</summary>
/// <remarks>
/// <b>It refuses rather than sending unauthenticated.</b> A request to an origin the policy did not approve means
/// the transport is wired somewhere the token was never meant to go, and letting it leave without the header would
/// turn that into a confusing refusal at the other end while hiding the mis-wiring.
/// </remarks>
internal sealed class McpAccessTokenHandler : DelegatingHandler
{
    private readonly CatalogueServerRequestPolicy _catalogueServerRequestPolicy;
    private readonly ILogger<McpAccessTokenHandler> _logger;
    private readonly IMcpAccessTokenService? _legacyTokens;

    public McpAccessTokenHandler(
        CatalogueServerRequestPolicy catalogueServerRequestPolicy,
        ILogger<McpAccessTokenHandler> logger)
        : this(catalogueServerRequestPolicy, logger, null)
    {
    }

    /// <summary>Compatibility path for tests written before the token moved to the run context.</summary>
    public McpAccessTokenHandler(
        IMcpAccessTokenService tokens,
        CatalogueServerRequestPolicy catalogueServerRequestPolicy,
        ILogger<McpAccessTokenHandler> logger)
        : this(catalogueServerRequestPolicy, logger, tokens)
    {
    }

    private McpAccessTokenHandler(
        CatalogueServerRequestPolicy catalogueServerRequestPolicy,
        ILogger<McpAccessTokenHandler> logger,
        IMcpAccessTokenService? legacyTokens)
    {
        _legacyTokens = legacyTokens;
        _catalogueServerRequestPolicy = catalogueServerRequestPolicy;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage httpRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpRequest);

        if (httpRequest.RequestUri is not { } requestUri
            || !_catalogueServerRequestPolicy.MayCarryTheCallersCatalogueAccessToken(requestUri))
        {
            throw new InvalidOperationException(
                $"An MCP request to '{httpRequest.RequestUri}' is not one the caller's token may be " +
                "attached to.");
        }

        var token = _legacyTokens?.Token ?? McpRunScope.Current?.Token;

        _logger.LogInformation(
            "Chat call prepared: no MCP tools; caller token {Token}.",
            string.IsNullOrEmpty(token) ? "absent" : "present");

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // The origin and the method are safe to record and the token is not, so only those two are named.
        _logger.LogDebug(
            "Attached the caller's token to {Method} {Origin}.",
            httpRequest.Method,
            requestUri.GetLeftPart(UriPartial.Authority));

        var runId = Activity.Current?.GetTagItem(WorkspaceTelemetry.RunId) as string;
        using var activity = WorkspaceTelemetry.ActivitySource.StartActivity("mcp.http", ActivityKind.Client);
        activity?.SetTag(WorkspaceTelemetry.RunId, runId);
        activity?.SetTag("http.method", httpRequest.Method.Method);
        activity?.SetTag("http.host", requestUri.Host);

        var response = await base.SendAsync(httpRequest, cancellationToken);
        activity?.SetTag("http.status_code", (int)response.StatusCode);

        if (response.Headers.TryGetValues("Server-Timing", out var timings))
        {
            activity?.SetTag("mcp.server_timing", string.Join(",", timings));
        }

        return response;
    }
}
