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
        var script = await factory.CreateClient().GetStringAsync("/swagger/index.js");

        var marker = "var oauthConfigObject = JSON.parse('";
        var start = script.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var config = script[start..script.IndexOf("')", start, StringComparison.Ordinal)];

        config.Should().Contain("public-client");
        config.Should().NotContain("sign-in-client");
    }
}
