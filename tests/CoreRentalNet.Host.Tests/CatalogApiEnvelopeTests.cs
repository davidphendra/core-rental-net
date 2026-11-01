using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using CoreRentalNet.Host.Configs;
using CoreRentalNet.Host.Controllers;
using CoreRentalNet.Host.Infrastructure;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The envelope's two numbers: how many products matched, and how many this answer carries.
/// </summary>
/// <remarks>
/// They are the same number until an answer is capped, and a caller that cannot tell them apart cannot
/// tell a complete answer from a partial one. That matters to the caller this endpoint exists for: what
/// it does with the rows depends on knowing it has all of them, and a partial answer would change the
/// candidate set it reasons over without saying so.
/// </remarks>
public sealed class CatalogApiEnvelopeTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{

    [Fact] // API-37
    public async Task A_limit_that_could_not_return_anything_is_a_400()
    {
        // Refused rather than clamped: quietly turning a caller's zero into one row is the kind of
        // answer that looks like it worked.
        var response = await factory.CreateClient().GetAsync("/api/catalog?limit=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("code").GetString().Should().Be(ApiErrorCode.InvalidRequest);
    }

    private async Task<JsonElement> EnvelopeAsync(string url)
    {
        var response = await factory.CreateClient().GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.Clone();
    }
}
