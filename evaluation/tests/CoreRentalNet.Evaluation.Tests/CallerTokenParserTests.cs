using System.Text.Json;
using AwesomeAssertions;
using CoreRentalNet.Evaluation.Execution;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests;

public sealed class CallerTokenParserTests
{
    [Fact]
    public void The_claims_a_preflight_needs_are_read_from_the_payload()
    {
        var value = Jwt(new
        {
            exp = 1_800_000_000,
            iss = "http://127.0.0.1:8090",
            aud = new[] { "core-rental-e2e" },
            permissions = new[] { "read:catalog", "use:ai" },
        });

        var token = CallerTokenParser.Parse(value);

        token.ExpiresAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1_800_000_000));
        token.Issuer.Should().Be("http://127.0.0.1:8090");
        token.Audiences.Should().Equal("core-rental-e2e");
        token.Permissions.Should().Equal("read:catalog", "use:ai");
    }

    [Fact]
    public void A_single_audience_string_is_read_as_one_audience()
    {
        var token = CallerTokenParser.Parse(Jwt(new { aud = "core-rental-e2e" }));

        token.Audiences.Should().Equal("core-rental-e2e");
    }

    [Fact]
    public void A_value_that_is_not_a_jwt_is_refused_by_name()
    {
        var act = () => CallerTokenParser.Parse("not-a-jwt");

        act.Should().Throw<CallerTokenException>().WithMessage("*JWT*");
    }

    private static string Jwt(object payload)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(payload);

        return $"e30.{Convert.ToBase64String(json).TrimEnd('=').Replace('+', '-').Replace('/', '_')}.";
    }
}
