using System.Net;
using AwesomeAssertions;
using CoreRentalNet.Host.Controllers;
using CoreRentalNet.Host.Helpers;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// A real request writes exactly one catalogue line, with the caller it was made as.
/// </summary>
public sealed class CatalogApiLoggingTests(CatalogApiAuthorizedFactory factory)
    : IClassFixture<CatalogApiAuthorizedFactory>
{
    [Fact] // API-12
    public async Task A_request_writes_one_line_with_the_caller_the_filters_and_the_count()
    {
        factory.Logs.Clear();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/catalog?category=desk");
        request.Headers.Add(CatalogApiTestHandler.PermissionsHeader, "read:catalog");

        var response = await factory.CreateClient().SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var entry = factory.Logs.Entries
            .Where(candidate => candidate.Category == CatalogApiLogHelper.Category)
            .Should().ContainSingle().Subject;

        entry.Property("Caller").Should().Be(CatalogApiTestHandler.Caller);
        entry.Property("Category").Should().Be("desk");
        entry.Property("Count").Should().Be(25);
    }

    [Fact] // API-08, API-12
    public async Task A_refused_request_writes_no_catalogue_line()
    {
        factory.Logs.Clear();

        var response = await factory.CreateClient().GetAsync("/api/catalog");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        factory.Logs.Entries
            .Where(candidate => candidate.Category == CatalogApiLogHelper.Category)
            .Should().BeEmpty("a refused call is not recorded as a read");
    }
}
