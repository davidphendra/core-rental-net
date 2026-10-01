using Auth0.AspNetCore.Authentication;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace CoreRentalNet.Host.Tests.Identity;

/// <summary>
/// The identity SDK is configured to validate the access token and refresh it, which is the whole of the
/// application's expiry handling.
/// </summary>
/// <remarks>
/// Asserted over the SDK's own options, because that is where the behaviour lives: this application reads no
/// `exp`, compares no times and calls no token endpoint.
/// </remarks>
public sealed class Auth0RefreshTokenOptionsTests(Auth0IdentityFactory factory) : IClassFixture<Auth0IdentityFactory>
{
    [Fact]
    public void The_access_token_is_refreshed_rather_than_left_to_lapse()
    {
        var withAccessToken = factory.Services
            .GetRequiredService<IOptionsMonitor<Auth0WebAppWithAccessTokenOptions>>()
            .Get(Auth0Constants.AuthenticationScheme);

        withAccessToken.UseRefreshTokens.Should().BeTrue(
            "the SDK can only keep the access token valid if it is allowed to exchange the refresh token");
        withAccessToken.AccessTokenExpirationLeeway.Should().Be(
            TimeSpan.FromMinutes(3),
            "the deployment names the margin in Auth0:LeewaySeconds, and a run streams for longer than the SDK's 60-second default");
    }

    [Fact] // the scope is what makes Auth0 issue a refresh token, and the SDK adds it when refreshing is enabled
    public void The_login_asks_for_the_scope_that_makes_a_refresh_token_possible()
    {
        var openIdConnect = factory.Services
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get(Auth0Constants.AuthenticationScheme);

        openIdConnect.Scope.Should().Contain(
            "offline_access",
            "without it Auth0 issues no refresh token, and an expired access token cannot be renewed");
    }

    [Fact] // the two failure paths end the session instead of letting an expired token be used
    public void Both_refresh_failure_paths_are_handled()
    {
        var withAccessToken = factory.Services
            .GetRequiredService<IOptionsMonitor<Auth0WebAppWithAccessTokenOptions>>()
            .Get(Auth0Constants.AuthenticationScheme);

        withAccessToken.Events.Should().NotBeNull(
            "a session that cannot refresh must be ended and sent back to sign in, not left looking valid");
        withAccessToken.Events!.OnMissingRefreshToken.Should().NotBeNull();
        withAccessToken.Events.OnAccessTokenRefreshFailed.Should().NotBeNull();
    }
}
