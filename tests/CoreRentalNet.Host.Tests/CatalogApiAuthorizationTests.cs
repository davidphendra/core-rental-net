using System.Net;
using AwesomeAssertions;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The catalogue endpoint with a provider configured: it answers only a caller that presents a token
/// carrying the configured permission.
/// </summary>
public sealed class CatalogApiAuthorizationTests(CatalogApiAuthorizedFactory factory)
    : IClassFixture<CatalogApiAuthorizedFactory>
{
    [Fact] // API-08
    public async Task Without_a_token_it_refuses_with_401()
    {
        var response = await factory.CreateClient().GetAsync("/api/catalog");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact] // API-09
    public async Task With_a_token_that_lacks_the_permission_it_forbids_with_403()
    {
        var response = await SendAsync("read:something-else");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact] // API-09
    public async Task A_permission_value_that_merely_contains_the_configured_one_is_forbidden()
    {
        var response = await SendAsync("read:catalog:everything");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact] // API-10
    public async Task With_the_configured_permission_it_answers_with_the_catalogue()
    {
        var response = await SendAsync("read:catalog");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().StartWith("[").And.Contain("\"category\"");
    }

    private async Task<HttpResponseMessage> SendAsync(string permissions)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/catalog");
        request.Headers.Add(CatalogApiTestHandler.PermissionsHeader, permissions);

        return await factory.CreateClient().SendAsync(request);
    }
}
