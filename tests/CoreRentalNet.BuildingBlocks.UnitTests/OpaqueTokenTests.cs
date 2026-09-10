using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using Xunit;

namespace CoreRentalNet.BuildingBlocks.UnitTests;

public sealed class OpaqueTokenTests
{
    [Fact] // DR-05, SEC-02
    public void The_stored_value_is_a_hash_of_the_raw_token_and_not_the_token()
    {
        var raw = OpaqueToken.IssueRawToken();

        var token = OpaqueToken.FromRawToken(raw);

        token.Hash.Should().NotBe(raw);
        token.Hash.Should().HaveLength(64);
        token.Hash.Should().Be(OpaqueToken.HashOf(raw));
    }

    [Fact]
    public void Issued_tokens_are_unique_and_carry_enough_entropy_to_be_unguessable()
    {
        var issued = Enumerable.Range(0, 500).Select(_ => OpaqueToken.IssueRawToken()).ToArray();

        issued.Should().OnlyHaveUniqueItems();
        issued.Should().OnlyContain(token => token.Length >= 40, "32 random bytes, base64url encoded");
    }

    [Fact]
    public void The_same_raw_token_always_hashes_the_same_way()
    {
        OpaqueToken.FromRawToken("abc").Should().Be(OpaqueToken.FromRawToken("abc"));
        OpaqueToken.FromRawToken("abc").Should().NotBe(OpaqueToken.FromRawToken("abd"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_token_is_refused(string raw)
    {
        var action = () => OpaqueToken.FromRawToken(raw);

        action.Should().Throw<DomainRuleViolationException>();
    }

    [Theory]
    [InlineData("nonsense")]
    [InlineData("0123456789abcdef")]
    public void Something_that_is_not_a_hash_is_refused(string hash)
    {
        var action = () => OpaqueToken.FromHash(hash);

        action.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void A_stored_hash_can_be_rehydrated()
    {
        var token = OpaqueToken.FromRawToken("raw-value");

        OpaqueToken.FromHash(token.Hash).Should().Be(token);
    }

    [Fact]
    public void A_token_with_too_little_entropy_is_refused()
    {
        var action = () => OpaqueToken.IssueRawToken(8);

        action.Should().Throw<DomainRuleViolationException>().WithMessage("*entropy*");
    }
}
