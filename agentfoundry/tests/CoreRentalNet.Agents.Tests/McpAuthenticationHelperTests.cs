using System.Security.Cryptography.X509Certificates;
using AwesomeAssertions;
using CoreRentalNet.Agents.Shared.Mcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>What the catalogue transport protects the caller's token with, asserted without a network.</summary>
/// <remarks>
/// The MAF sample's security notes, turned into facts: no cookie jar on a per-call client, no automatic redirect
/// that could carry the bearer to an origin the policy never approved, a certificate whose revocation is checked,
/// and a pooled connection that does not outlive the token that opened it.
/// </remarks>
public sealed class McpAuthenticationHelperTests
{
    private const string TheCatalogueServer = "https://catalogue.example/mcp";

    [Fact]
    public void The_token_handler_delegates_to_the_terminal_handler_that_protects_it()
    {
        using var handler = HandlerOverTheCatalogue();

        handler.InnerHandler.Should().BeOfType<SocketsHttpHandler>()
            .Which.UseCookies.Should().BeFalse(
                "a cookie jar is state shared between calls on a transport that exists for one call");
    }

    [Fact]
    public void The_terminal_handler_refuses_a_redirect_and_checks_revocation()
    {
        using var handler = HandlerOverTheCatalogue();
        var terminalHandler = (SocketsHttpHandler)handler.InnerHandler!;

        terminalHandler.AllowAutoRedirect.Should().BeFalse(
            "a redirect could carry the bearer to an origin the policy never approved");
        terminalHandler.PooledConnectionLifetime.Should().Be(
            TimeSpan.FromMinutes(2),
            "a connection that outlives the token that opened it is worth nothing");
        terminalHandler.SslOptions.CertificateRevocationCheckMode.Should().Be(X509RevocationMode.Online);
    }

    [Fact]
    public void The_client_gives_one_catalogue_call_the_stated_budget()
    {
        using var client = McpAuthenticationHelper.BuildTheClientThatPresentsTheCallersToken(
            HolderHoldingNothing(),
            PolicyFor(TheCatalogueServer),
            NullLoggerFactory.Instance);

        client.Timeout.Should().Be(
            TimeSpan.FromMinutes(5),
            "a similarity search embeds the sentence and scans the index, and the transport default is too short");
    }

    private static CallerCatalogueAccessTokenAttachmentHandler HandlerOverTheCatalogue()
        => McpAuthenticationHelper.BuildTheTokenAttachmentHandler(
            HolderHoldingNothing(),
            PolicyFor(TheCatalogueServer),
            NullLoggerFactory.Instance);

    private static CatalogueServerRequestPolicy PolicyFor(string mcpEndpoint)
        => new(new CallerAuthorisedMcpSettings(mcpEndpoint));

    private static McpAccessTokenService HolderHoldingNothing()
        => new(NullLogger<McpAccessTokenService>.Instance);
}
