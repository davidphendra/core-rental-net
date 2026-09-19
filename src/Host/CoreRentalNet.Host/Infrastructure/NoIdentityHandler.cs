using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CoreRentalNet.Host.Infrastructure;

/// <summary>
/// The scheme a deployment has when it has no identity provider at all, so that a permission which is
/// closed can be refused rather than failing to answer.
/// </summary>
/// <remarks>
/// <para>
/// This application's permissions answer for themselves when nobody can be checked: the catalogue opens,
/// because reading it costs nothing, and the AI builder is <b>closed</b>, because a run is paid for and a
/// deployment that has not said who may run one has not been finished. A closed permission therefore fails
/// authorisation - and the authorization middleware, having failed a caller it does not consider
/// authenticated, then asks to <em>challenge</em>.
/// </para>
/// <para>
/// A deployment with no provider has nothing to challenge with, and the framework raises
/// <see cref="InvalidOperationException"/> when asked for a default challenge scheme that does not exist.
/// The customer's answer was a 500: the closed feature looked like a broken one. This scheme is what a
/// challenge falls back on, and it exists only in that deployment - with a provider configured the
/// provider's own schemes are registered and this is not.
/// </para>
/// <para>
/// <b>Its challenge forbids rather than challenging.</b> A 401 asks the caller to authenticate and come
/// back, and there is no credential this deployment could ever accept, so that instruction cannot be
/// followed by anyone. What is true is that the feature is closed here, and that is a 403. Nothing is
/// written: the API's problem-details service supplies the body, so this refusal reads like every other.
/// </para>
/// <para>
/// It never authenticates anybody. It has no ticket to issue - there is no provider - so it answers "no
/// result" and leaves the caller anonymous, which is what the permission has already accounted for.
/// </para>
/// </remarks>
internal sealed class NoIdentityHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>The scheme's name, registered once here and referenced only where it is registered.</summary>
    public const string SchemeName = "no-identity";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        => Task.FromResult(AuthenticateResult.NoResult());

    /// <summary>Refuses, because there is nobody who could be asked to authenticate.</summary>
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        => RefuseAsync();

    /// <summary>Refuses, for the same reason and with the same answer as a challenge.</summary>
    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        => RefuseAsync();

    private Task RefuseAsync()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;

        return Task.CompletedTask;
    }
}
