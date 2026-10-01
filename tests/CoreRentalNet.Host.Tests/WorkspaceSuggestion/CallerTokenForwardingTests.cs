using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using CoreRentalNet.Host.Configs;
using Xunit;

namespace CoreRentalNet.Host.Tests.WorkspaceSuggestion;

/// <summary>The token a run is handed is the one the identity SDK produced, not an expired one from the ticket.</summary>
/// <remarks>
/// The endpoint no longer reads the ticket directly: it asks the SDK, which validates the recorded expiry and
/// refreshes when it has passed. This pins that the value reaching the run is that answered token.
/// </remarks>
public sealed class CallerTokenForwardingTests
{
    [Fact]
    public async Task The_run_receives_the_token_the_identity_sdk_produced()
    {
        var agent = new CapturingSuggestionAgentAdapter();

        using var factory = new SuggestionEndpointFactory(agent);

        var request = new HttpRequestMessage(HttpMethod.Post, BuilderRoutes.Suggest)
        {
            Content = JsonContent.Create(new { query = "a desk and a chair" }),
        };

        request.Headers.Add(CatalogApiTestHandler.PermissionsHeader, "builder:ai");

        var response = await factory.CreateClient().SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        agent.CallerAccessToken.Should().Be(
            CatalogApiTestHandler.AccessToken,
            "the token the SDK produced is the one the run is handed");
    }
}
