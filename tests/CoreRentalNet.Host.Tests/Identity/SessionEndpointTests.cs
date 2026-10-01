using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using CoreRentalNet.Host.Configs;
using CoreRentalNet.Host.Presentation;
using CoreRentalNet.Host.Tests.WorkspaceSuggestion;
using Xunit;

namespace CoreRentalNet.Host.Tests.Identity;

/// <summary>The session-status endpoint: what the identity SDK will answer, as a body the panel can read.</summary>
/// <remarks>
/// It reports; it does not decide. A session whose token is valid answers <c>canRunSuggestion: true</c>, and one
/// the SDK cannot produce a token for answers false - which is the difference the panel words as a re-login.
/// </remarks>
public sealed class SessionEndpointTests(SuggestionEndpointFactory factory)
    : IClassFixture<SuggestionEndpointFactory>
{
    [Fact]
    public async Task A_session_that_can_produce_a_token_can_run()
    {
        var status = await GetStatusAsync(withAccessToken: true);

        status.CanRunSuggestion.Should().BeTrue("the SDK produced a token, so a suggestion run can read the catalogue");
    }

    [Fact] // the session is still signed in; it just has nothing to present
    public async Task A_session_that_cannot_produce_a_token_cannot_run()
    {
        var status = await GetStatusAsync(withAccessToken: false);

        status.CanRunSuggestion.Should().BeFalse(
            "the SDK could not produce a token, which is what the panel turns into 'sign in again'");
    }

    [Fact] // the prefix is what puts this endpoint behind the API's problem-details pipeline
    public void The_route_is_under_the_api_prefix()
        => SessionRoutes.Status.Should().StartWith(CatalogRoutes.Prefix);

    private async Task<SessionStatus> GetStatusAsync(bool withAccessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, SessionRoutes.Status);

        request.Headers.Add(CatalogApiTestHandler.PermissionsHeader, "read:catalog");

        if (!withAccessToken)
        {
            request.Headers.Add(CatalogApiTestHandler.WithoutAccessTokenHeader, "true");
        }

        var response = await factory.CreateClient().SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<SessionStatus>())!;
    }
}
