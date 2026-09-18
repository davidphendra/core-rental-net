using System.Net;
using System.Text.Json;
using AwesomeAssertions;
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
    [Fact] // API-36
    public async Task A_request_that_names_no_limit_is_complete_and_says_so()
    {
        var body = await EnvelopeAsync("/api/catalog");

        body.GetProperty("count").GetInt32().Should().Be(205);
        body.GetProperty("total").GetInt32().Should().Be(205);
        body.GetProperty("truncated").GetBoolean().Should().BeFalse();

        // The default has to be above the catalogue's present size, or an unqualified request would
        // silently start truncating as the catalogue grew.
        CatalogApiLimits.Default.Should().BeGreaterThan(205);
    }

    [Fact] // API-37
    public async Task A_capped_answer_carries_fewer_rows_than_matched_and_says_so()
    {
        var body = await EnvelopeAsync("/api/catalog?limit=5");

        body.GetProperty("value").GetArrayLength().Should().Be(5);
        body.GetProperty("count").GetInt32().Should().Be(5);
        body.GetProperty("total").GetInt32().Should().Be(205);
        body.GetProperty("truncated").GetBoolean().Should().BeTrue();
    }

    [Fact] // API-37
    public async Task The_total_counts_what_the_filters_matched_not_the_catalogue()
    {
        var body = await EnvelopeAsync("/api/catalog?subCategory=monitor&limit=5");

        body.GetProperty("count").GetInt32().Should().Be(5);
        body.GetProperty("total").GetInt32().Should().Be(30);
        body.GetProperty("truncated").GetBoolean().Should().BeTrue();
    }

    [Fact] // API-38
    public async Task Truncation_is_decided_at_the_boundary_rather_than_reported_flatly()
    {
        // Non-vacuity for API-37: an answer that reported `truncated: true` whenever a limit was sent,
        // rather than comparing the two numbers, would satisfy the test above and fail this one. The
        // catalogue holds 205, so one either side of that is the boundary.
        var exact = await EnvelopeAsync("/api/catalog?limit=205");
        exact.GetProperty("count").GetInt32().Should().Be(205);
        exact.GetProperty("total").GetInt32().Should().Be(205);
        exact.GetProperty("truncated").GetBoolean().Should().BeFalse();

        var capped = await EnvelopeAsync("/api/catalog?limit=204");
        capped.GetProperty("count").GetInt32().Should().Be(204);
        capped.GetProperty("total").GetInt32().Should().Be(205);
        capped.GetProperty("truncated").GetBoolean().Should().BeTrue();
    }

    [Fact] // API-37
    public async Task The_compact_answer_reports_the_same_two_numbers()
    {
        var body = await EnvelopeAsync("/api/catalog?view=compact&limit=3");

        body.GetProperty("count").GetInt32().Should().Be(3);
        body.GetProperty("total").GetInt32().Should().Be(205);
        body.GetProperty("truncated").GetBoolean().Should().BeTrue();
    }

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
