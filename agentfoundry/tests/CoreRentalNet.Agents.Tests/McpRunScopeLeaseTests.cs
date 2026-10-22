using AwesomeAssertions;
using CoreRentalNet.Agents.Shared.Mcp;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The ambient token context exists only while the catalogue operation that needs it runs.</summary>
public sealed class McpRunScopeLeaseTests
{
    [Fact]
    public void Entering_a_scope_makes_the_token_visible()
    {
        using var tokenScope = McpRunScope.Enter("the-callers-access-token");

        McpRunScope.Current.Should().NotBeNull();
        McpRunScope.Current!.Token.Should().Be("the-callers-access-token");
    }

    [Fact]
    public void Disposing_a_scope_restores_the_previous_context()
    {
        using (McpRunScope.Enter("the-callers-access-token"))
        {
            McpRunScope.Current!.Token.Should().Be("the-callers-access-token");
        }

        McpRunScope.Current.Should().BeNull();
    }

    [Fact]
    public void Nested_scopes_restore_in_order()
    {
        using (McpRunScope.Enter("the-outer-token"))
        {
            using (McpRunScope.Enter("the-inner-token"))
            {
                McpRunScope.Current!.Token.Should().Be("the-inner-token");
            }

            McpRunScope.Current!.Token.Should().Be("the-outer-token");
        }

        McpRunScope.Current.Should().BeNull();
    }

    [Fact]
    public void A_null_token_clears_the_context()
    {
        using var tokenScope = McpRunScope.Enter(null);

        McpRunScope.Current.Should().BeNull();
    }

    [Fact]
    public void An_empty_token_clears_the_context()
    {
        using var tokenScope = McpRunScope.Enter("   ");

        McpRunScope.Current.Should().BeNull();
    }
}
