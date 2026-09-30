using AwesomeAssertions;
using CoreRentalNet.Agents.Shared.Mcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// The holder's own behaviour: it gives the caller's token up the moment it is told the run has finished with it,
/// and it cannot keep one past the call either way.
/// </summary>
/// <remarks>
/// <b>Two releases, and they are tested separately because they are different promises.</b> The explicit
/// <c>Release</c> is the pipeline saying the run is over — the point a reader can find and a test can assert.
/// <c>Dispose</c> is the call saying it is over, which is what keeps a holder from outliving the request that
/// carried the token even on a path that never reaches an ending.
/// </remarks>
public sealed class McpAccessTokenServiceTests
{
    private const string TheCallersToken = "header.eyJzdWIiOiJjdXN0b21lci0xIn0.signature";

    [Fact]
    public void Releasing_takes_the_token_out_of_memory()
    {
        var accessTokens = HolderHoldingTheCallersToken();

        accessTokens.Release();

        accessTokens.Token.Should().BeNull("the run has finished with it, so nothing should still be able to read it");
    }

    [Fact] // and a stage that reaches for it after the ending fails loudly rather than silently presenting nothing
    public async Task A_token_that_was_released_is_not_handed_out_again()
    {
        var accessTokens = HolderHoldingTheCallersToken();
        accessTokens.Release();

        var act = async () => await accessTokens.GetAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact] // the backstop: a path that never reaches an ending cannot keep the credential either
    public void Disposing_the_holder_takes_the_token_out_of_memory()
    {
        using var accessTokens = HolderHoldingTheCallersToken();

        accessTokens.Dispose();

        accessTokens.Token.Should().BeNull();
    }

    [Fact] // releasing twice is not an event, so a second ending cannot make the log read like a leak
    public void Releasing_a_holder_that_holds_nothing_is_harmless()
    {
        var accessTokens = HolderHoldingTheCallersToken();
        accessTokens.Release();

        accessTokens.Release();

        accessTokens.Token.Should().BeNull();
    }

    private static McpAccessTokenService HolderHoldingTheCallersToken()
        => new(NullLogger<McpAccessTokenService>.Instance) { Token = TheCallersToken };
}
