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

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
