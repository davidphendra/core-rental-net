using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using CoreRentalNet.Host.Controllers;
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

        parameters.Should().BeEquivalentTo(["category", "subCategory", "search", "view", "limit"]);

        // The compact projection cannot be a second schema on the same status and content type, so the
        // parameter carries the vocabulary instead. This is what makes the projection discoverable.
        var described = operation.GetProperty("parameters").EnumerateArray()
            .Where(parameter => parameter.TryGetProperty("description", out _))
            .ToDictionary(
                parameter => parameter.GetProperty("name").GetString()!,
                parameter => parameter.GetProperty("description").GetString()!);

        described["view"].Should().Contain("compact").And.Contain("full");

        // A caller cannot guess what leaving the cap out means, so the document says it.
        described["limit"].Should().Contain("matched").And.Contain(CatalogApiLimits.Default.ToString());

        var responses = operation.GetProperty("responses");
        responses.TryGetProperty("200", out _).Should().BeTrue();
        responses.TryGetProperty("400", out _).Should().BeTrue();
    }

    [Fact] // API-13, API-18
    public async Task The_document_names_the_content_type_each_answer_arrives_as()
    {
        using var document = await GetDocumentAsync();

        var responses = document.RootElement
            .GetProperty("paths").GetProperty("/api/catalog").GetProperty("get").GetProperty("responses");

        // Named rather than left to the formatters: without this the document offers text/plain and
        // text/json beside the one shape the endpoint sends.
        ContentTypes(responses.GetProperty("200")).Should().Equal("application/json");
        ContentTypes(responses.GetProperty("400")).Should().Equal("application/problem+json");

        foreach (var status in new[] { "401", "403", "404", "405", "500" })
        {
            ContentTypes(responses.GetProperty(status)).Should().Equal("application/problem+json");

            // And the model behind it, so a generated client raises the same error type for a refusal
            // whatever refused it.
            responses.GetProperty(status).GetProperty("content")
                .GetProperty("application/problem+json").GetProperty("schema")
                .GetProperty("$ref").GetString()!.Should().EndWith("ProblemDetails");
        }
    }

    [Fact] // API-13, API-17
    public async Task The_document_writes_the_item_vocabulary_as_strings()
    {
        using var document = await GetDocumentAsync();
        var root = document.RootElement;

        var success = root
            .GetProperty("paths").GetProperty("/api/catalog").GetProperty("get")
            .GetProperty("responses").GetProperty("200")
            .GetProperty("content").GetProperty("application/json").GetProperty("schema");

        // The document describes the answer the endpoint actually sends - the envelope, with the
        // products inside it - so the vocabulary below is reached the way a client reaches it.
        var envelope = Resolve(root, success);

        // The envelope's own members: exactly the ones the endpoint sends, so a field added to it is a
        // deliberate change to the contract rather than something that appeared in the document.
        envelope.GetProperty("properties").EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo(["value", "count", "total", "truncated"]);

        var items = Resolve(root, envelope.GetProperty("properties").GetProperty("value").GetProperty("items"));
        var category = Resolve(root, items.GetProperty("properties").GetProperty("category"));

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

    [Fact] // API-18
    public async Task Only_the_gated_operation_requires_the_scheme()
    {
        using var document = await GetDocumentAsync();

        var paths = document.RootElement.GetProperty("paths");

        paths.GetProperty("/api/catalog").GetProperty("get").TryGetProperty("security", out _).Should().BeTrue();

        // The account routes are not gated, and declaring a token requirement on a sign-in would
        // describe a flow that cannot happen.
        foreach (var ungated in new[] { "/account/login", "/account/logout" })
        {
            paths.TryGetProperty(ungated, out var path).Should().BeTrue();

            foreach (var operation in path.EnumerateObject())
            {
                operation.Value.TryGetProperty("security", out _).Should().BeFalse($"'{ungated}' is not gated");
            }
        }
    }

    [Fact] // API-18
    public async Task The_requirement_is_the_permission_the_gate_checks()
    {
        using var document = await GetDocumentAsync();

        var scopes = document.RootElement
            .GetProperty("paths").GetProperty("/api/catalog").GetProperty("get").GetProperty("security")
            .EnumerateArray()
            .SelectMany(requirement => requirement.GetProperty("Auth0").EnumerateArray())
            .Select(scope => scope.GetString())
            .ToArray();

        // A generated client would be made to ask for the login's scopes if this were identity.Scope.
        scopes.Should().Equal("read:catalog");

        // And every scope the flow offers says what it is; the specification asks for a description.
        foreach (var scope in document.RootElement
            .GetProperty("components").GetProperty("securitySchemes").GetProperty("Auth0")
            .GetProperty("flows").GetProperty("authorizationCode").GetProperty("scopes")
            .EnumerateObject())
        {
            scope.Value.GetString().Should().NotBeNullOrWhiteSpace($"scope '{scope.Name}' has no description");
        }
    }

    private async Task<JsonDocument> GetDocumentAsync()
    {
        var response = await factory.CreateClient().GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();

        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    /// <summary>The media types a documented response declares.</summary>
    private static string[] ContentTypes(JsonElement response)
        => [.. response.GetProperty("content").EnumerateObject().Select(media => media.Name)];

    /// <summary>The schema a property points at, following a <c>$ref</c> when it is written as one.</summary>
    private static JsonElement Resolve(JsonElement document, JsonElement schema)
        => schema.TryGetProperty("$ref", out var reference)
            ? document.GetProperty("components").GetProperty("schemas")
                .GetProperty(reference.GetString()!.Split('/')[^1])
            : schema;
}
