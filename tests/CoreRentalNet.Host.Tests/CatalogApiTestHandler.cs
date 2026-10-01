using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// A test scheme that answers with the permissions named in a request header, so the endpoint's
/// authorization can be exercised without minting a token.
/// </summary>
/// <remarks>
/// Registered by the test host only, under its own name, so the real bearer scheme is left alone. A
/// request with no header is unauthenticated, which is what makes the 401 case reachable.
/// </remarks>
internal sealed class CatalogApiTestHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "CatalogApiTest";

    public const string PermissionsHeader = "X-Test-Permissions";

    /// <summary>The subject every authenticated test request is made as.</summary>
    public const string Caller = "test-client";

    /// <summary>The access token the test sign-in saves, which a run forwards to the agent.</summary>
    /// <remarks>
    /// A real sign-in saves one and the run endpoint now requires it: a run without the caller's token cannot read
    /// the catalogue, so a request that carries none is refused before it is paid for.
    /// </remarks>
    public const string AccessToken = "header.eyJzdWIiOiJ0ZXN0LWNsaWVudCJ9.signature";

    /// <summary>The header a test sends to be signed in with no token for this API.</summary>
    public const string WithoutAccessTokenHeader = "X-Test-Without-Access-Token";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(PermissionsHeader, out var values) || values.Count == 0)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = values
            .SelectMany(value => value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [])
            .Select(permission => new Claim("permissions", permission))
            .Append(new Claim("sub", Caller));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        var properties = new AuthenticationProperties();

        // The ticket carries the token the way a sign-in cookie carries the one the identity provider issued,
        // because that is where the run endpoint reads it from.
        if (!Request.Headers.ContainsKey(WithoutAccessTokenHeader))
        {
            properties.StoreTokens(
            [
                new AuthenticationToken { Name = "access_token", Value = AccessToken },

                // The identity SDK reads the ticket's recorded expiry before it will hand the token out, so a
                // sign-in it is asked about has to state one that has not passed. Without it the SDK treats the
                // token as expired and goes looking for a refresh token this test never provides.
                new AuthenticationToken
                {
                    Name = "expires_at",
                    Value = DateTimeOffset.Now.AddHours(1).ToString("o"),
                },
            ]);
        }

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, properties, SchemeName)));
    }
}
