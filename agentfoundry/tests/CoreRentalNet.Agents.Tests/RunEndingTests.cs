using AwesomeAssertions;
using CoreRentalNet.Agents.Shared.Mcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The order a run ends in: the authenticated session closes before the token that opened it is given up.</summary>
/// <remarks>
/// This is the property whose absence made the transport's own session-termination request fail with "The request
/// carried no MCP access token". The ending is deliberately lopsided — connection first, token second — and the
/// order is the whole of what is asserted here.
/// </remarks>
public sealed class RunEndingTests
{
    [Fact]
    public async Task The_run_ends_by_closing_the_catalogue_session_before_giving_up_the_token()
    {
        var tokens = new McpAccessTokenService(NullLogger<McpAccessTokenService>.Instance)
        {
            Token = "the-callers-token",
        };

        var connection = new RecordingMcpAuthorizationConnection(tokens);

        var runScope = new StubRunScope(
            (typeof(IMcpAuthorizationConnection), connection),
            (typeof(IMcpAccessTokenService), tokens));

        await new RunEnding(runScope).EndTheRunAsync();

        connection.TokenWasPresentWhenClosed.Should().BeTrue(
            "the transport's session-termination request is authenticated with the caller's token");
        tokens.Token.Should().BeNull(
            "the token is given up only after the session that presents it is closed");
    }
}
