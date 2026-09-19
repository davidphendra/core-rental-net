using System.Net;
using AwesomeAssertions;
using CoreRentalNet.Host.Controllers;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The endpoint a suggestion run is started from, with identity off - which is the state a deployment is
/// in when nobody has said who may use the builder.
/// </summary>
/// <remarks>
/// The refusal is worth a test of its own because the AI permission is <b>closed</b> when the deployment
/// has not named its claim, and a permission that refuses has to refuse in a refusal - a status a caller
/// can read and act on - rather than by failing to answer at all. That distinction is invisible in a diff
/// and is the difference between a closed feature and a broken one.
/// </remarks>
public sealed class SuggestionEndpointTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    [Fact] // e05s07: the endpoint the money is spent at is gated in its own right
    public async Task With_no_identity_provider_it_refuses_rather_than_running_a_suggestion()
    {
        using var response = await factory.CreateClient().PostAsync(BuilderRoutes.Suggest, content: null);

        response.StatusCode.Should().Be(
            HttpStatusCode.Forbidden,
            "there is no provider, so nobody can ever be entitled and the permission is closed");

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }
}
