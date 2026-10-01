using Auth0.AspNetCore.Authentication;
using AwesomeAssertions;
using CoreRentalNet.Host.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CoreRentalNet.Host.Tests.Infrastructure;

/// <summary>The run path's token: what the identity SDK vouches for, or a failure a caller turns into a re-login.</summary>
/// <remarks>
/// The real SDK over a real request, because the whole point of the service is that the SDK decides - a fake of
/// its answer would prove nothing about expiry or refresh. The token endpoint is the one thing stood in for, so
/// no provider is reached.
/// </remarks>
public sealed class CallerAccessTokenServiceTests
{
    private const string TheAudience = "https://corerental/api";

    [Fact]
    public async Task The_service_returns_the_token_the_session_already_holds()
    {
        var tokenEndpoint = ScriptedTokenEndpointHandler.Issuing("refreshed.token");
        await using var provider = ProviderFor(tokenEndpoint);
        var httpContext = RequestFor(provider);

        var token = await ServiceFor(httpContext).GetForTheRunAsync();

        token.Should().Be(CallerAccessTokenTestHandler.AccessToken);
        tokenEndpoint.WasCalled.Should().BeFalse(
            "the recorded expiry is well ahead of the SDK's margin, so nothing had to be refreshed");
    }

    [Fact]
    public async Task The_service_returns_the_refreshed_token_when_the_recorded_expiry_is_inside_the_margin()
    {
        var tokenEndpoint = ScriptedTokenEndpointHandler.Issuing("refreshed.token");
        await using var provider = ProviderFor(tokenEndpoint);
        var httpContext = RequestFor(provider);
        httpContext.Request.Headers[CallerAccessTokenTestHandler.ExpiresInSecondsHeader] = "30";
        httpContext.Request.Headers[CallerAccessTokenTestHandler.WithRefreshTokenHeader] = "true";

        var token = await ServiceFor(httpContext).GetForTheRunAsync();

        token.Should().Be("refreshed.token");
        tokenEndpoint.WasCalled.Should().BeTrue(
            "30 seconds left is inside the SDK's margin, so the refresh token was exchanged");
    }

    [Fact]
    public async Task The_service_throws_when_the_session_cannot_produce_a_token()
    {
        var tokenEndpoint = ScriptedTokenEndpointHandler.Issuing("refreshed.token");
        await using var provider = ProviderFor(tokenEndpoint);
        var httpContext = RequestFor(provider);
        httpContext.Request.Headers[CallerAccessTokenTestHandler.WithoutAccessTokenHeader] = "true";

        Func<Task> read = async () => await ServiceFor(httpContext).GetForTheRunAsync();

        await read.Should().ThrowAsync<CallerAccessTokenUnavailableException>();
    }

    [Fact]
    public async Task The_service_throws_when_the_token_endpoint_refuses_the_refresh()
    {
        var tokenEndpoint = ScriptedTokenEndpointHandler.RefusingTheRefreshToken();
        await using var provider = ProviderFor(tokenEndpoint);
        var httpContext = RequestFor(provider);
        httpContext.Request.Headers[CallerAccessTokenTestHandler.ExpiresInSecondsHeader] = "-60";
        httpContext.Request.Headers[CallerAccessTokenTestHandler.WithRefreshTokenHeader] = "true";

        Func<Task> read = async () => await ServiceFor(httpContext).GetForTheRunAsync();

        await read.Should().ThrowAsync<CallerAccessTokenUnavailableException>();
        tokenEndpoint.WasCalled.Should().BeTrue("the refresh was attempted before it was refused");
    }

    [Fact]
    public async Task The_service_throws_when_there_is_no_request_to_read_the_token_from()
    {
        Func<Task> read = async () => await new CallerAccessTokenService(new HttpContextAccessor()).GetForTheRunAsync();

        await read.Should().ThrowAsync<InvalidOperationException>();
    }

    private static CallerAccessTokenService ServiceFor(HttpContext httpContext)
        => new(new HttpContextAccessor { HttpContext = httpContext });

    private static DefaultHttpContext RequestFor(IServiceProvider provider)
        => new() { RequestServices = provider };

    /// <summary>The identity SDK over the test sign-in, with a scripted token endpoint and no provider reachable.</summary>
    private static ServiceProvider ProviderFor(HttpMessageHandler tokenEndpoint)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddOptions();
        services.AddAuthentication(CallerAccessTokenTestHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, CallerAccessTokenTestHandler>(
                CallerAccessTokenTestHandler.SchemeName,
                _ => { });

        services.Configure<Auth0WebAppOptions>(Auth0Constants.AuthenticationScheme, options =>
        {
            options.Domain = "tenant.example";
            options.ClientId = "client";
            options.CookieAuthenticationScheme = CallerAccessTokenTestHandler.SchemeName;
            options.Backchannel = new HttpClient(tokenEndpoint);
        });

        services.Configure<Auth0WebAppWithAccessTokenOptions>(Auth0Constants.AuthenticationScheme, options =>
        {
            options.Audience = TheAudience;
            options.UseRefreshTokens = true;

            // A null answer is asserted as the service's own throw rather than as a sign-out, so the events that
            // would end a real session are left unset in a test.
            options.Events = null;
        });

        return services.BuildServiceProvider();
    }
}
