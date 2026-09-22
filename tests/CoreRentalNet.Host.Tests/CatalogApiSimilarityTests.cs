using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The catalogue's similarityService search over a real request: its own permission, the filters, the envelope, and
/// the one refusal a caller may retry.
/// </summary>
/// <remarks>
/// The scoring is exercised in the integration tier over a real vector file. What is asserted here is the
/// endpoint: that the second capability really is gated separately from reading the catalogue, that the
/// envelope's two numbers still mean what they mean, and that an unusable index is a 503 rather than an empty
/// answer or a 500.
/// </remarks>
public sealed class CatalogApiSimilarityTests(CatalogApiSimilarityFactory factory)
    : IClassFixture<CatalogApiSimilarityFactory>
{
    [Fact] // API-39
    public async Task With_the_catalogue_permission_but_not_this_one_it_forbids_with_403()
    {
        var response = await SendAsync("read:catalog", "/api/catalog/similarity?query=two+screens");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        problem.RootElement.GetProperty("code").GetString().Should().Be(ApiErrorCode.NotPermitted);
    }

    [Fact] // API-39
    public async Task Without_a_token_it_refuses_with_401()
    {
        var response = await factory.CreateClient().GetAsync("/api/catalog/similarity?query=two+screens");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        problem.RootElement.GetProperty("code").GetString().Should().Be(ApiErrorCode.Unauthenticated);
    }

    [Fact] // API-40
    public async Task With_the_permission_it_answers_nearest_first()
    {
        var scored = ScoredSkus();

        var response = await SendAsync("searchsimilarity:aibuilder", "/api/catalog/similarity?query=two+screens");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var body = document.RootElement;

        Items(body).Select(item => item.GetProperty("sku").GetString()).Should().Equal(scored);
        body.GetProperty("count").GetInt32().Should().Be(scored.Length);
        body.GetProperty("total").GetInt32().Should().Be(scored.Length);
        body.GetProperty("truncated").GetBoolean().Should().BeFalse();
    }

    [Fact] // API-40
    public async Task A_limit_caps_the_answer_and_the_total_still_says_what_matched()
    {
        var scored = ScoredSkus();

        var response = await SendAsync(
            "searchsimilarity:aibuilder",
            "/api/catalog/similarity?query=two+screens&limit=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var body = document.RootElement;

        Items(body).Should().HaveCount(1);
        body.GetProperty("value")[0].GetProperty("sku").GetString().Should().Be(scored[0]);
        body.GetProperty("count").GetInt32().Should().Be(1);
        body.GetProperty("total").GetInt32().Should().Be(scored.Length);
        body.GetProperty("truncated").GetBoolean().Should().BeTrue();
    }

    [Fact] // API-40
    public async Task The_compact_projection_is_available_here_too()
    {
        var response = await SendAsync(
            "searchsimilarity:aibuilder",
            "/api/catalog/similarity?query=two+screens&view=compact");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var body = document.RootElement;

        body.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(
            ["value", "count", "total", "truncated", "currency"]);
        body.GetProperty("currency").GetString().Should().Be("IDR");
    }

    [Fact] // API-40
    public async Task A_sentence_that_was_not_sent_is_a_400()
    {
        // A similarityService search with nothing to be similar to is not a search: refused by the framework before the
        // action runs, exactly as an unknown filter is.
        var response = await SendAsync("searchsimilarity:aibuilder", "/api/catalog/similarity");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        problem.RootElement.GetProperty("code").GetString().Should().Be(ApiErrorCode.InvalidRequest);
    }

    [Fact] // API-01
    public async Task An_unknown_filter_is_refused_the_same_way_the_published_view_refuses_it()
    {
        var response = await SendAsync(
            "searchsimilarity:aibuilder",
            "/api/catalog/similarity?query=two+screens&category=sofa");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        problem.RootElement.GetProperty("code").GetString().Should().Be(ApiErrorCode.UnknownFilter);
    }

    [Fact] // API-41
    public async Task Vectors_that_cannot_be_searched_are_a_503_a_caller_may_retry()
    {
        using var unavailable = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IProductSimilarityService>();
            services.AddSingleton<IProductSimilarityService, UnavailableProductSimilarityService>();
        }));

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/catalog/similarity?query=two+screens");
        request.Headers.Add(CatalogApiTestHandler.PermissionsHeader, "searchsimilarity:aibuilder");

        var response = await unavailable.CreateClient().SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = problem.RootElement;

        root.GetProperty("code").GetString().Should().Be(ApiErrorCode.SimilarityUnavailable);
        root.GetProperty("status").GetInt32().Should().Be(503);
        root.GetProperty("instance").GetString().Should().Be("/api/catalog/similarity");
        root.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>The SKUs the stand-in scores, in the order it scores them.</summary>
    private string[] ScoredSkus()
        => [.. ((TestProductSimilarityService)factory.Services.GetRequiredService<IProductSimilarityService>()).Skus];

    private static IEnumerable<JsonElement> Items(JsonElement body)
        => body.GetProperty("value").EnumerateArray();

    private async Task<HttpResponseMessage> SendAsync(string permissions, string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add(CatalogApiTestHandler.PermissionsHeader, permissions);

        return await factory.CreateClient().SendAsync(request);
    }
}
