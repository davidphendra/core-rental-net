using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using CoreRentalNet.Host.Infrastructure;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The catalogue endpoint over a real request: routing, the query string, the status codes and the
/// JSON an agent actually receives.
/// </summary>
/// <remarks>
/// In process rather than in a browser, because the contract lives at the HTTP boundary and a unit
/// test cannot see the serializer, the route or the default status codes.
/// </remarks>
public sealed class CatalogApiEndpointTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    [Fact] // API-17
    public async Task The_answer_is_an_envelope_whose_count_is_its_own_length()
    {
        var response = await factory.CreateClient().GetAsync("/api/catalog");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var body = document.RootElement;

        // Exactly these two, so a field added to the answer is a deliberate change to the contract
        // rather than something that appeared in it.
        body.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(["value", "count"]);
        body.GetProperty("value").GetArrayLength().Should().Be(62);
        body.GetProperty("count").GetInt32().Should().Be(62);
    }

    [Fact] // API-03
    public async Task No_filter_returns_the_whole_catalogue_in_catalog_order()
    {
        var products = await GetArrayAsync("/api/catalog");

        products.GetArrayLength().Should().Be(62);
        products[0].GetProperty("name").GetString().Should().Be("Seminyak Lounge");
    }

    [Fact] // API-04
    public async Task A_category_narrows_to_that_category()
    {
        var products = await GetArrayAsync("/api/catalog?category=desk");

        products.GetArrayLength().Should().Be(10);
        products.EnumerateArray().Should().OnlyContain(
            product => product.GetProperty("category").GetString() == "desk");
    }

    [Fact] // API-04
    public async Task A_subcategory_narrows_to_that_subcategory()
    {
        var products = await GetArrayAsync("/api/catalog?subCategory=monitor");

        products.GetArrayLength().Should().Be(8);
        products.EnumerateArray().Should().OnlyContain(
            product => product.GetProperty("subCategory").GetString() == "monitor");
    }

    [Fact] // API-02
    public async Task A_filter_is_matched_ignoring_case()
    {
        var products = await GetArrayAsync("/api/catalog?category=DESK");

        products.GetArrayLength().Should().Be(10);
    }

    [Fact] // API-05
    public async Task A_search_narrows_by_name_only()
    {
        var products = await GetArrayAsync("/api/catalog?search=teak");

        products.GetArrayLength().Should().BeGreaterThan(0);
        products.EnumerateArray().Should().OnlyContain(
            product => product.GetProperty("name").GetString()!.Contains("Teak", StringComparison.OrdinalIgnoreCase));
    }

    [Fact] // API-06
    public async Task No_match_is_an_empty_list_not_a_404()
    {
        var response = await factory.CreateClient().GetAsync("/api/catalog?search=zzzzzzzz");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        document.RootElement.GetProperty("value").GetArrayLength().Should().Be(0);
        document.RootElement.GetProperty("count").GetInt32().Should().Be(0);
    }

    [Fact] // API-01, API-18
    public async Task An_unknown_category_is_a_400_naming_the_allowed_values()
    {
        var response = await factory.CreateClient().GetAsync("/api/catalog?category=sofa");

        // The framework's own error shape, not a hand-written string: a caller that already parses a
        // problem-details response needs no special case for this endpoint.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var body = problem.RootElement;

        body.GetProperty("status").GetInt32().Should().Be(400);
        body.GetProperty("instance").GetString().Should().Be("/api/catalog");

        // Branch on this, not on the prose: the code is the part that is contract.
        body.GetProperty("code").GetString().Should().Be(ApiErrorCode.UnknownFilter);

        var refusal = body.GetProperty("errors").GetProperty("category")[0].GetString()!;
        refusal.Should().Contain("sofa").And.Contain("chair").And.Contain("desk").And.Contain("accessory");
    }

    [Fact] // API-01
    public async Task An_unknown_subcategory_is_refused_the_same_way()
    {
        var response = await factory.CreateClient().GetAsync("/api/catalog?subCategory=hammock");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = problem.RootElement;

        root.GetProperty("code").GetString().Should().Be(ApiErrorCode.UnknownFilter);

        var refusal = root.GetProperty("errors").GetProperty("subCategory")[0].GetString()!;
        refusal.Should().Contain("hammock").And.Contain("monitor").And.Contain("lamp");
    }

    [Fact] // API-19
    public async Task An_address_under_the_api_that_answers_nothing_is_a_404_problem_detail()
    {
        var response = await factory.CreateClient().GetAsync("/api/no-such-thing");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        problem.RootElement.GetProperty("code").GetString().Should().Be(ApiErrorCode.UnknownRoute);
        problem.RootElement.GetProperty("status").GetInt32().Should().Be(404);
        problem.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact] // API-20
    public async Task A_method_the_route_does_not_answer_is_a_405_problem_detail()
    {
        var response = await factory.CreateClient().PostAsync("/api/catalog", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        problem.RootElement.GetProperty("code").GetString().Should().Be(ApiErrorCode.MethodNotAllowed);
    }

    [Fact] // API-23
    public async Task An_address_that_is_not_an_api_route_is_not_answered_with_json()
    {
        var response = await factory.CreateClient().GetAsync("/no-such-page");

        // The error shape above is scoped to the API. A browser that mistyped an address is a page
        // request, and answering it with problem details would be a defect of its own - it is the
        // reason the middleware is mounted for the prefix rather than for the application.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().NotBe("application/problem+json");
    }

    [Fact] // API-07
    public async Task The_body_is_camel_case_with_lowercase_enum_strings_and_a_money_object()
    {
        var products = await GetArrayAsync("/api/catalog?category=desk");
        var first = products[0];

        first.GetProperty("sku").GetString().Should().NotBeNullOrWhiteSpace();
        first.TryGetProperty("name", out _).Should().BeTrue();
        first.TryGetProperty("monthlyPrice", out var price).Should().BeTrue();
        price.GetProperty("amount").GetDecimal().Should().BeGreaterThan(0);
        price.GetProperty("currency").GetString().Should().Be("IDR");
        first.GetProperty("subCategory").ValueKind.Should().Be(JsonValueKind.Null);
        first.GetProperty("category").GetString().Should().Be("desk");
    }

    [Fact] // API-13
    public async Task The_openapi_document_is_not_served_outside_development()
    {
        var response = await factory.CreateClient().GetAsync("/openapi/v1.json");

        // The document names every route and shape the application has. Development is where a
        // caller needs it; anywhere else it is a map handed to whoever asks.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact] // API-15
    public async Task The_swagger_ui_is_not_served_outside_development()
    {
        var response = await factory.CreateClient().GetAsync("/swagger/index.html");

        // The page tries the API from a browser. It belongs where a developer is working, and it is
        // the same surface the relaxed policy is scoped to, so both stop at the same place.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>The items of a collection answer, which is where every catalogue assertion reads.</summary>
    private async Task<JsonElement> GetArrayAsync(string path)
    {
        var response = await factory.CreateClient().GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.GetProperty("value").Clone();
    }
}
