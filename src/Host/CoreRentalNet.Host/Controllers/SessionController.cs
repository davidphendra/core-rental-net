using Auth0.AspNetCore.Authentication;
using CoreRentalNet.Host.Configs;
using CoreRentalNet.Host.Presentation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreRentalNet.Host.Controllers;

/// <summary>Reports whether this session can still produce a catalogue token, as the identity SDK sees it.</summary>
/// <remarks>
/// <para>
/// <b>It reports the SDK's answer; it does not decide it.</b> The SDK validates the access token's recorded
/// expiry, exchanges the session's refresh token when it has lapsed, and returns null when it cannot. That null
/// is the whole of the detection, and this controller only turns it into a body the panel can read - so a run
/// that would be refused is refused before it is paid for rather than at the catalogue's far end.
/// </para>
/// <para>
/// <b>Internal, for the same reason <see cref="BuilderController"/> is:</b> MVC discovers it through
/// <c>InternalControllerFeatureProvider</c>, so a controller that names no module type can still be internal.
/// </para>
/// <para>
/// <b>It holds no state and needs no injected dependency.</b> Everything it asks is on <c>HttpContext</c>, which
/// is the only place the sign-in session exists - a Blazor circuit has none, which is why the panel asks this
/// endpoint rather than reading a token itself.
/// </para>
/// </remarks>
[ApiController]
[Route(SessionRoutes.Status)]
[Authorize]
internal sealed class SessionController : ControllerBase
{
    /// <summary>The session's ability to run a suggestion, and when the session itself ends.</summary>
    [HttpGet]
    [ProducesResponseType<SessionStatus>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<SessionStatus>> Status()
    {
        // The SDK's read: a valid token now, a refreshed one when the recorded expiry has passed, or null when
        // there is nothing to refresh with. No `exp` parsing, no time comparison and no token endpoint call.
        var accessToken = await HttpContext.GetAccessTokenAsync(new AccessTokenRequest())
            .ConfigureAwait(false);

        // The SDK's enforced upstream ceiling, or null when the connection did not emit one. Reading it does not
        // change any expiry behaviour.
        var sessionExpiryUnixSeconds = await HttpContext.GetSessionExpiryAsync().ConfigureAwait(false);

        return new SessionStatus(
            CanRunSuggestion: !string.IsNullOrWhiteSpace(accessToken),
            SessionExpiresAt: sessionExpiryUnixSeconds is { } ceiling
                ? DateTimeOffset.FromUnixTimeSeconds(ceiling)
                : null);
    }
}
