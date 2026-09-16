using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.BuildingBlocks.Domain;
using Xunit;

namespace CoreRentalNet.BuildingBlocks.UnitTests;

/// <summary>
/// Issuing and hashing, now in <see cref="OpaqueTokenService"/>.
/// </summary>
public sealed class OpaqueTokenTests
{
    private static readonly IOpaqueTokenService Tokens = new OpaqueTokenService();

    [Fact] // DR-05, SEC-02
    public void The_stored_value_is_a_hash_of_the_raw_token_and_not_the_token()
    {
        var raw = Tokens.IssueRawToken();

        var hash = Tokens.HashOf(raw);

        hash.Should().NotBe(raw);
        hash.Should().HaveLength(64);
        hash.Should().Be(Tokens.HashOf(raw));
    }

    [Fact]
    public void Issued_tokens_are_unique_and_carry_enough_entropy_to_be_unguessable()
    {
        var issued = Enumerable.Range(0, 500).Select(_ => Tokens.IssueRawToken()).ToArray();

        issued.Should().OnlyHaveUniqueItems();
        issued.Should().OnlyContain(token => token.Length >= 40, "32 random bytes, base64url encoded");
    }

    [Fact]
    public void The_same_raw_token_always_hashes_the_same_way()
    {
        Tokens.HashOf("abc").Should().Be(Tokens.HashOf("abc"));
        Tokens.HashOf("abc").Should().NotBe(Tokens.HashOf("abd"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_token_is_refused(string raw)
    {
        var action = () => Tokens.HashOf(raw);

        action.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void A_token_with_too_little_entropy_is_refused()
    {
        var action = () => Tokens.IssueRawToken(8);

        action.Should().Throw<DomainRuleViolationException>().WithMessage("*entropy*");
    }
}
