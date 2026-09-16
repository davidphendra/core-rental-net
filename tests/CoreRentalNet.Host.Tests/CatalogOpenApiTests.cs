using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The machine-readable description of the catalogue endpoint: what makes it discoverable without
/// reading the code, and what a generated client would be built from.
/// </summary>
public sealed class CatalogOpenApiTests(CatalogOpenApiFactory factory) : IClassFixture<CatalogOpenApiFactory>
{
    [Fact] // API-13
    public async Task The_document_is_served_in_development()
    {
        var response = await factory.CreateClient().GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact] // API-13
    public async Task The_document_describes_the_route_its_filters_and_its_answers()
    {
        using var document = await GetDocumentAsync();

        var operation = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/catalog")
            .GetProperty("get");

        var parameters = operation.GetProperty("parameters").EnumerateArray()
            .Select(parameter => parameter.GetProperty("name").GetString())
            .ToArray();

        parameters.Should().BeEquivalentTo(["category", "subCategory", "search"]);

        var responses = operation.GetProperty("responses");
        responses.TryGetProperty("200", out _).Should().BeTrue();
        responses.TryGetProperty("400", out _).Should().BeTrue();
    }

    [Fact] // API-13
    public async Task The_document_writes_the_filter_vocabulary_as_strings()
    {
        using var document = await GetDocumentAsync();

        var product = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas")
            .EnumerateObject()
            .Single(schema => schema.Name.Contains("ProductView", StringComparison.Ordinal))
            .Value;

        var category = Resolve(document.RootElement, product.GetProperty("properties").GetProperty("category"));

        // A generated client and a machine reader both send back what they read, so the vocabulary is
        // asserted as the words themselves: described as numbers, a caller would be left looking for a
        // mapping that is not the contract.
        category.GetProperty("enum").EnumerateArray()
            .Where(value => value.ValueKind == JsonValueKind.String)
            .Select(value => value.GetString())
            .Should().BeEquivalentTo(["chair", "desk", "accessory"]);
    }

    [Fact] // API-14
    public async Task The_document_declares_how_a_caller_obtains_a_token()
    {
        using var document = await GetDocumentAsync();

        var scheme = document.RootElement
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("Auth0");

        scheme.GetProperty("type").GetString().Should().Be("oauth2");

        var flow = scheme.GetProperty("flows").GetProperty("authorizationCode");

        // Both addresses come from the identity configuration, so a deployment that points at another
        // provider describes that provider rather than the one this repository was written against.
        flow.GetProperty("authorizationUrl").GetString().Should().Be("https://tenant.example/authorize");
        flow.GetProperty("tokenUrl").GetString().Should().Be("https://tenant.example/oauth/token");

        // The scope the flow asks for is the permission the gate checks; a scope that was never
        // requested is a permission the provider will not issue.
        flow.GetProperty("scopes").TryGetProperty("read:catalog", out _).Should().BeTrue();
    }

    [Fact] // API-14
    public async Task The_endpoint_requires_the_scheme_the_document_declares()
    {
        using var document = await GetDocumentAsync();

        var security = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/catalog")
            .GetProperty("get")
            .GetProperty("security");

        var schemes = security.EnumerateArray()
            .SelectMany(requirement => requirement.EnumerateObject().Select(property => property.Name))
            .Distinct()
            .ToArray();

        schemes.Should().Equal(["Auth0"], "the endpoint must point at the scheme the document declares");
    }

    private async Task<JsonDocument> GetDocumentAsync()
    {
        var response = await factory.CreateClient().GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();

        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    /// <summary>The schema a property points at, following a <c>$ref</c> when it is written as one.</summary>
    private static JsonElement Resolve(JsonElement document, JsonElement schema)
        => schema.TryGetProperty("$ref", out var reference)
            ? document.GetProperty("components").GetProperty("schemas")
                .GetProperty(reference.GetString()!.Split('/')[^1])
            : schema;
}
