using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Workspace.Domain;
using Xunit;

namespace CoreRentalNet.Modules.Workspace.UnitTests;

public sealed class DraftTokenTests
{
    [Fact] // DR-05
    public void The_stored_value_is_a_hash_of_the_raw_token_not_the_token_itself()
    {
        var raw = DraftToken.IssueRawToken();

        var token = DraftToken.FromRawToken(raw);

        token.Hash.Should().NotBe(raw);
        token.Hash.Should().HaveLength(64);
        token.Hash.Should().Be(DraftToken.HashOf(raw));
    }

    [Fact] // DR-05
    public void The_same_raw_token_always_hashes_the_same_way()
    {
        DraftToken.FromRawToken("abc").Should().Be(DraftToken.FromRawToken("abc"));
        DraftToken.FromRawToken("abc").Should().NotBe(DraftToken.FromRawToken("abd"));
    }

    [Fact] // DR-05
    public void Issued_tokens_are_unique_and_long_enough_to_be_unguessable()
    {
        var issued = Enumerable.Range(0, 200).Select(_ => DraftToken.IssueRawToken()).ToArray();

        issued.Should().OnlyHaveUniqueItems();
        issued.Should().OnlyContain(token => token.Length >= 40, "32 random bytes base64url encoded");
    }

    [Fact]
    public void An_empty_token_is_refused()
    {
        var action = () => DraftToken.FromRawToken("   ");

        action.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void A_hash_that_is_not_a_hash_is_refused()
    {
        var action = () => DraftToken.FromHash("nonsense");

        action.Should().Throw<DomainRuleViolationException>();
    }
}
