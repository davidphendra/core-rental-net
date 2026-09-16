using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using CoreRentalNet.Host.Infrastructure;
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

        // A refused caller is told which refusal it was, in a value it can branch on, and given the
        // identifier to quote. An empty body left it guessing at all three.
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        problem.RootElement.GetProperty("code").GetString().Should().Be(ApiErrorCode.Unauthenticated);
        problem.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact] // API-09, API-21
    public async Task With_a_token_that_lacks_the_permission_it_forbids_with_403()
    {
        var response = await SendAsync("read:something-else");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        problem.RootElement.GetProperty("code").GetString().Should().Be(ApiErrorCode.NotPermitted);
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

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        document.RootElement.GetProperty("count").GetInt32().Should().Be(62);
        document.RootElement.GetProperty("value")[0].GetProperty("category").GetString().Should().NotBeNullOrWhiteSpace();
    }

    private async Task<HttpResponseMessage> SendAsync(string permissions)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/catalog");
        request.Headers.Add(CatalogApiTestHandler.PermissionsHeader, permissions);

        return await factory.CreateClient().SendAsync(request);
    }
}
