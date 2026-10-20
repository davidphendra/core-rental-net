using System.Net.Http.Headers;

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

        return await base.SendAsync(httpRequest, cancellationToken);
    }
}
