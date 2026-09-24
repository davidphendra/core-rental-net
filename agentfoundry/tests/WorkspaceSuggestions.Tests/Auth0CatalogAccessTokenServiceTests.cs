using AwesomeAssertions;
using WorkspaceSuggestions.Tools;
using Xunit;

namespace WorkspaceSuggestions.Tests;

/// <summary>
/// The catalogue's tokenService is fetched once, held, and refreshed before it expires.
/// </summary>
/// <remarks>
/// No tokenService endpoint is reached: the handler is a stand-in, so the rule under test - when a tokenService is reused -
/// is exercised without a network or a clock being moved.
/// </remarks>
public sealed class Auth0CatalogAccessTokenServiceTests
{
    private static readonly CatalogToolSettings Settings = new(
        McpEndpoint: "https://catalogue.example/mcp",
        TokenEndpoint: "https://tenant.example/oauth/tokenService",
        Audience: "https://catalogue.example",
        ClientId: "client-id",
        ClientSecret: "client-secret");

    [Fact]
    public async Task A_token_that_is_still_fresh_is_not_fetched_again()
    {
        var handler = new CountingTokenHandler(expiresIn: 3600);
        var token = new Auth0CatalogAccessTokenService(Settings, new HttpClient(handler));

        var first = await token.GetAsync(CancellationToken.None);
        var second = await token.GetAsync(CancellationToken.None);

        first.Should().Be(second);
        handler.Requests.Should().Be(1, "one tokenService covers many tool calls");
    }

    [Fact]
    public async Task A_token_inside_the_refresh_margin_is_fetched_again()
    {
        var handler = new CountingTokenHandler(expiresIn: 0);
        var token = new Auth0CatalogAccessTokenService(Settings, new HttpClient(handler));

        await token.GetAsync(CancellationToken.None);
        await token.GetAsync(CancellationToken.None);

        handler.Requests.Should().Be(2, "a tokenService that expires within the margin is stale before it is used");
    }

    [Fact]
    public async Task The_grant_names_the_audience_the_catalogue_checks()
    {
        var handler = new CountingTokenHandler(expiresIn: 3600);
        var token = new Auth0CatalogAccessTokenService(Settings, new HttpClient(handler));

        await token.GetAsync(CancellationToken.None);

        handler.LastBody.Should()
            .Contain("grant_type=client_credentials")
            .And.Contain("audience=https%3A%2F%2Fcatalogue.example");
    }
}
