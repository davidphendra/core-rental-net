using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreRentalNet.Host.Tests.Infrastructure;

/// <summary>A test sign-in the identity SDK can both read and write, so its refresh path can be exercised.</summary>
/// <remarks>
/// <para>
/// Distinct from the shared <c>CatalogApiTestHandler</c>, which is a read-only sign-in: a refresh that succeeds
/// ends by writing the rotated token back through <c>SignInAsync</c>, and the base handler that one uses does
/// not accept a write. This one is a <see cref="SignInAuthenticationHandler{TOptions}"/>, whose sign-in and
/// sign-out are no-ops that succeed - enough for the SDK to finish a refresh with no provider and no cookie.
/// </para>
/// <para>
/// The ticket's shape is stated by request headers, as the other test sign-in states its permissions, so a test
/// arranges one sign-in without a second container.
/// </para>
/// </remarks>
internal sealed class CallerAccessTokenTestHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : SignInAuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "CallerAccessTokenTest";

    /// <summary>The access token the ticket carries, which a run is expected to be handed.</summary>
    public const string AccessToken = "header.eyJzdWIiOiJ0ZXN0LWNsaWVudCJ9.signature";

    /// <summary>The refresh token the ticket carries when a test asks for one.</summary>
    public const string RefreshToken = "test-refresh-token";

    /// <summary>The header stating, in seconds from now, when the ticket's token is recorded as expiring.</summary>
    public const string ExpiresInSecondsHeader = "X-Test-Access-Token-Expires-In-Seconds";

    /// <summary>The header a test sends to be signed in with no access token for this API.</summary>
    public const string WithoutAccessTokenHeader = "X-Test-Without-Access-Token";

    /// <summary>The header a test sends to have the ticket carry a refresh token.</summary>
    public const string WithRefreshTokenHeader = "X-Test-With-Refresh-Token";

    /// <summary>The lifetime a ticket records when a test states none: comfortably ahead of the SDK's margin.</summary>
    private const int DefaultExpiresInSeconds = 3600;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "test-client")], SchemeName));
        var properties = new AuthenticationProperties();

        if (!Request.Headers.ContainsKey(WithoutAccessTokenHeader))
        {
            properties.StoreTokens(TokensOfTheTicket());
        }

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, properties, SchemeName)));
    }

    protected override Task HandleSignInAsync(ClaimsPrincipal user, AuthenticationProperties? properties)
        => Task.CompletedTask;

    protected override Task HandleSignOutAsync(AuthenticationProperties? properties)
        => Task.CompletedTask;

    private IEnumerable<AuthenticationToken> TokensOfTheTicket()
    {
        yield return new AuthenticationToken { Name = "access_token", Value = AccessToken };
        yield return new AuthenticationToken
        {
            Name = "expires_at",
            Value = DateTimeOffset.Now.AddSeconds(ExpiresInSeconds()).ToString("o"),
        };

        if (Request.Headers.ContainsKey(WithRefreshTokenHeader))
        {
            yield return new AuthenticationToken { Name = "refresh_token", Value = RefreshToken };
        }
    }

    private int ExpiresInSeconds()
        => Request.Headers.TryGetValue(ExpiresInSecondsHeader, out var values)
           && int.TryParse(values.ToString(), out var seconds)
            ? seconds
            : DefaultExpiresInSeconds;
}
