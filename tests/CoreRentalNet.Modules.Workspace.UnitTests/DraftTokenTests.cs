using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Workspace.Domain;
using Xunit;

namespace CoreRentalNet.Modules.Workspace.UnitTests;

/// <summary>
/// The draft token is a plain record now; issuing and hashing live in
/// <see cref="OpaqueTokenService"/> and are covered in BuildingBlocks.
/// </summary>
public sealed class DraftTokenTests
{
    private static readonly IOpaqueTokenService Tokens = new OpaqueTokenService();

    [Fact] // DR-05
    public void A_draft_token_carries_the_hash_of_its_raw_token_not_the_token_itself()
    {
        var raw = Tokens.IssueRawToken();

        var token = new DraftToken(Tokens.HashOf(raw));

        token.Hash.Should().NotBe(raw);
        token.Hash.Should().HaveLength(64);
        token.Hash.Should().Be(Tokens.HashOf(raw));
    }

    [Fact] // DR-05
    public void The_same_raw_token_always_hashes_the_same_way()
    {
        new DraftToken(Tokens.HashOf("abc")).Should().Be(new DraftToken(Tokens.HashOf("abc")));
        new DraftToken(Tokens.HashOf("abc")).Should().NotBe(new DraftToken(Tokens.HashOf("abd")));
    }

    [Fact]
    public void An_empty_token_is_refused()
    {
        var action = () => Tokens.HashOf("   ");

        action.Should().Throw<DomainRuleViolationException>();
    }
}
