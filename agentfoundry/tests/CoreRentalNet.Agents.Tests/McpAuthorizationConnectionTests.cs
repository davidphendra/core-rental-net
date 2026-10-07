using AwesomeAssertions;
using CoreRentalNet.Agents.Shared.Mcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>A connection that opened nothing still has to be closeable, and closing it twice must not fail.</summary>
/// <remarks>
/// Every run ends through <see cref="RunEnding"/>, including one that never reached the catalogue; and the scope
/// disposes the connection again after the ending already closed it. Both are the same no-op and are asserted so
/// they stay one.
/// </remarks>
public sealed class McpAuthorizationConnectionTests
{
    [Fact]
    public async Task Closing_a_connection_that_opened_no_session_is_harmless_and_repeatable()
    {
        var connection = new McpAuthorizationConnection(
            new McpSetting(string.Empty),
            new McpAccessTokenService(NullLogger<McpAccessTokenService>.Instance),
            NullLoggerFactory.Instance,
            NullLogger<McpAuthorizationConnection>.Instance);

        await connection.CloseAsync();
        await connection.CloseAsync();
        await connection.DisposeAsync();

        connection.IsConfigured.Should().BeFalse("an empty endpoint is how the tests state an unconfigured catalogue");
    }
}
