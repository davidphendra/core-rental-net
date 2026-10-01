using AwesomeAssertions;
using CoreRentalNet.Agents.Shared.Mcp;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// Where the caller's catalogue token may be attached: the one server it was meant for, over a transport that
/// protects it.
/// </summary>
/// <remarks>
/// <b>Tested without a network, because the rule is a decision and not a request.</b> A bearer token is attached to
/// every request an MCP client makes, so the interesting cases are the ones where it must <i>not</i> be: another
/// origin, another port, the same host over a scheme nobody configured, and plain text to somewhere that is not
/// the loopback.
/// </remarks>
public sealed class CatalogueServerRequestPolicyTests
{
    private const string TheCatalogueServer = "https://catalogue.example/mcp";

    [Fact]
    public void The_configured_server_over_https_may_carry_the_token()
        => PolicyFor(TheCatalogueServer)
            .MayCarryTheCallersCatalogueAccessToken(new Uri("https://catalogue.example/mcp"))
            .Should().BeTrue();

    [Theory]
    [InlineData("https://elsewhere.example/mcp")]              // another host
    [InlineData("https://catalogue.example:8443/mcp")]         // the same host on another port
    [InlineData("http://catalogue.example/mcp")]               // the configured host over plain text
    [InlineData("ftp://catalogue.example/mcp")]                // not a transport a token belongs on
    public void A_request_the_catalogue_server_did_not_ask_for_may_not_carry_the_token(string requestUri)
        => PolicyFor(TheCatalogueServer)
            .MayCarryTheCallersCatalogueAccessToken(new Uri(requestUri))
            .Should().BeFalse("the token is a credential for one server, and it is not this one");

    [Fact] // the local catalogue is served over plain http, and only a loopback address may inherit that
    public void Plain_text_is_permitted_only_for_a_loopback_catalogue_server()
    {
        PolicyFor("http://127.0.0.1:5502/mcp")
            .MayCarryTheCallersCatalogueAccessToken(new Uri("http://127.0.0.1:5502/mcp"))
            .Should().BeTrue("the local catalogue is reached over http, and the endpoint naming a loopback is what permits it");
    }

    [Fact] // a loopback endpoint does not make some other plain-text host acceptable
    public void The_loopback_exception_does_not_travel_to_another_host()
        => PolicyFor("http://127.0.0.1:5502/mcp")
            .MayCarryTheCallersCatalogueAccessToken(new Uri("http://catalogue.example/mcp"))
            .Should().BeFalse("permission to use plain text is permission for the loopback, not for plain text");

    [Fact] // a deployment behind a name rather than an address: still https, still pinned
    public void The_loopback_exception_does_not_apply_to_a_named_https_endpoint()
        => PolicyFor("https://localhost:5502/mcp")
            .MayCarryTheCallersCatalogueAccessToken(new Uri("https://localhost:5502/mcp"))
            .Should().BeTrue("https protects the token whatever the host is called");

    private static CatalogueServerRequestPolicy PolicyFor(string mcpEndpoint)
        => new(new McpSetting(mcpEndpoint));
}
