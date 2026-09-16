using System.Net;
using System.Text.Json;
using AwesomeAssertions;
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
        (await response.Content.ReadAsStringAsync()).Trim().Should().Be("[]");
    }

    [Fact] // API-01
    public async Task An_unknown_category_is_a_400_naming_the_allowed_values()
    {
        var response = await factory.CreateClient().GetAsync("/api/catalog?category=sofa");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("sofa").And.Contain("chair").And.Contain("desk").And.Contain("accessory");
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

    private async Task<JsonElement> GetArrayAsync(string path)
    {
        var response = await factory.CreateClient().GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.Clone();
    }
}
