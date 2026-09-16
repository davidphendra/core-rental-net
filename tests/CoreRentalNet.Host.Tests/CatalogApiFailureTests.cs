using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Modules.Catalog.Application.Queries.SearchCatalog;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// What the API answers when something goes wrong that it did not plan for.
/// </summary>
/// <remarks>
/// The failure is made by replacing the catalogue with one that throws, rather than by adding a route
/// that throws: the request then travels the real pipeline — routing, authorization, the controller,
/// the exception handler — and what is asserted is what a caller would actually receive.
/// </remarks>
public sealed class CatalogApiFailureTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    [Fact] // API-22
    public async Task A_failed_request_is_a_500_problem_detail_that_says_nothing_about_the_failure()
    {
        using var failing = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ISearchCatalogHandler>();
            services.AddScoped<ISearchCatalogHandler, ThrowingCatalogHandler>();
        }));

        var response = await failing.CreateClient().GetAsync("/api/catalog");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var body = await response.Content.ReadAsStringAsync();

        using var problem = JsonDocument.Parse(body);
        var root = problem.RootElement;

        root.GetProperty("status").GetInt32().Should().Be(500);
        root.GetProperty("code").GetString().Should().Be(ApiErrorCode.Unhandled);
        root.GetProperty("instance").GetString().Should().Be("/api/catalog");

        // The one field that makes the failure reportable: the caller quotes this, and it is the
        // identifier the server logged the request under.
        root.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();

        // And nothing else. The exception is written for whoever runs the application, and it
        // routinely names a path, a query or a value the caller was not entitled to see.
        body.Should().NotContain(ThrowingCatalogHandler.Message);
        body.Should().NotContain(nameof(InvalidOperationException));
    }
}
