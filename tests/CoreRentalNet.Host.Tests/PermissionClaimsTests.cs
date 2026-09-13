using System.Security.Claims;
using System.Text;
using AwesomeAssertions;
using CoreRentalNet.Host.Presentation;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// Reading an account's permissions from the access token and lifting them into the identity.
/// </summary>
/// <remarks>
/// The access token is the only token Auth0 writes permissions to, so this is the only place they
/// can be read. The tests use unsigned tokens because the reading is all that is under test - the
/// token has already come over TLS from the provider's own token endpoint.
/// </remarks>
public sealed class PermissionClaimsTests
{
    private const string ClaimType = "https://corerental/permissions";

    [Fact] // AUTH-30
    public void The_access_tokens_permissions_are_read()
        => PermissionClaims.Read(Token("""{"permissions":["read:catalog","write:orders"]}"""))
            .Should().Equal("read:catalog", "write:orders");

    [Fact] // AUTH-30
    public void A_token_without_a_permissions_claim_carries_none()
        => PermissionClaims.Read(Token("""{"sub":"auth0|42"}""")).Should().BeEmpty();

    [Theory] // AUTH-30
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-token")]
    public void A_token_that_cannot_be_read_grants_nothing(string? token)
        => PermissionClaims.Read(token).Should().BeEmpty();

    [Fact] // AUTH-30
    public void They_become_claims_of_the_configured_type()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "test"));

        PermissionClaims.AddTo(principal, Token("""{"permissions":["read:catalog"]}"""), ClaimType);

        principal.HasClaim(ClaimType, "read:catalog").Should().BeTrue();
    }

    [Fact] // AUTH-30
    public void A_deployment_that_names_no_claim_is_written_nothing()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "test"));

        PermissionClaims.AddTo(principal, Token("""{"permissions":["read:catalog"]}"""), "  ");

        principal.Claims.Should().BeEmpty();
    }

    /// <summary>An unsigned JWT, which is enough to read a payload from.</summary>
    private static string Token(string payload)
        => $"{Segment("""{"alg":"none"}""")}.{Segment(payload)}.{Segment("signature")}";

    private static string Segment(string value)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
