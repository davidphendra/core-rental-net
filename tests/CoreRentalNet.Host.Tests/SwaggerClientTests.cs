using AwesomeAssertions;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// A public client named for the documentation page, without disturbing the client the application
/// signs people in with.
/// </summary>
public sealed class SwaggerClientTests(SwaggerClientFactory factory) : IClassFixture<SwaggerClientFactory>
{
    [Fact] // API-15
    public async Task Naming_a_client_for_the_page_leaves_the_sign_in_client_alone()
    {
        var config = await SwaggerTestSetup.InjectedConfigAsync(factory.CreateClient(), "oauthConfigObject");

        config.GetProperty("clientId").GetString().Should().Be("public-client");

        config.GetRawText().Should().NotContain(
            SwaggerClientFactory.SignInClient,
            "the page authorizes with its own client; the sign-in client is not on it");
    }
}
