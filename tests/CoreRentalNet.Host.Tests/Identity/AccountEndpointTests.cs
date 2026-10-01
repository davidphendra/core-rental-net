using System.Net;
using AwesomeAssertions;
using CoreRentalNet.Host.Controllers;
using Xunit;

namespace CoreRentalNet.Host.Tests.Identity;

/// <summary>What the account routes answer where there is no identity provider.</summary>
/// <remarks>
/// <para>
/// The two routes exist to start and end a session at a provider. Where a deployment has none, the honest
/// answer is the one routing gives by not publishing them at all, and this is the only place that answer is
/// written down: with a provider configured the routes belong to the browser suite, and
/// <c>CatalogOpenApiTests</c> already asserts they are present in the document and carry no gate.
/// </para>
/// <para>
/// <b>The refusal is problem details, and that is a deliberate change.</b> The routes used to be mapped only
/// where a provider was configured, so an unconfigured deployment answered them from routing: a bare 404 with
/// no body. Now that they are an action, <c>[ApiController]</c> maps the client error, so the same 404 arrives
/// with the API's refusal shape. The status is what the customer sees and it is unchanged; the body is asserted
/// so the change is stated rather than discovered.
/// </para>
/// <para>
/// The factory is the catalogue's only because it is the one that runs the application with identity off by
/// configuration rather than by having no credentials - a machine holding real ones would otherwise answer
/// these requests with a challenge.
/// </para>
/// </remarks>
public sealed class AccountEndpointTests(CatalogApiFactory factory)
    : IClassFixture<CatalogApiFactory>
{
    [Theory]
    [InlineData(AccountController.SignInPath)]
    [InlineData(AccountController.SignOutPath)]
    public async Task With_no_identity_provider_the_account_routes_refuse(string path)
    {
        var response = await factory.CreateClient().GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be(
            "application/problem+json",
            "the routes are an action now, so the framework writes the refusal in the API's own shape");
    }
}
