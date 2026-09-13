using System.Security.Claims;
using System.Text;
using AwesomeAssertions;
using CoreRentalNet.Host.Presentation;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// Reading the role out of the access token's permissions and lifting it into the identity.
/// </summary>
/// <remarks>
/// The tenant names one permission per role, so the role is the permission's name before the
/// <c>:role</c> suffix. The tests use unsigned tokens because the reading is all that is under test -
/// the token has already come over TLS from the provider's own token endpoint.
/// </remarks>
public sealed class RoleClaimsTests
{
    private const string ClaimType = "https://core-rental.periang.auth0/roles";

    [Theory] // AUTH-32
    [InlineData("manager:role", "Manager")]
    [InlineData("supervisor:role", "Supervisor")]
    [InlineData("staff:role", "Staff")]
    [InlineData("guest:role", "Guest")]
    public void The_role_named_by_a_permission_is_read(string permission, string expected)
        => Roles(Token($$"""{"permissions":["{{permission}}"]}""")).Should().Equal(expected);

    [Fact] // AUTH-32
    public void A_permission_that_names_no_role_is_left_alone()
        => Roles(Token("""{"permissions":["read:catalog","write:orders"]}""")).Should().BeEmpty();

    [Fact] // AUTH-32
    public void Every_role_the_token_names_is_kept()
        => Roles(Token("""{"permissions":["staff:role","manager:role"]}""")).Should().Equal("Staff", "Manager");

    [Fact] // AUTH-32
    public void A_token_with_no_permissions_names_no_role()
        => Roles(Token("""{"sub":"auth0|42"}""")).Should().BeEmpty();

    [Theory] // AUTH-32
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-token")]
    public void A_token_that_cannot_be_read_names_no_role(string? token)
        => Roles(token).Should().BeEmpty();

    [Fact] // AUTH-32
    public void A_deployment_that_names_no_claim_is_written_nothing()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "test"));

        RoleClaims.AddTo(principal, Token("""{"permissions":["manager:role"]}"""), "  ");

        principal.Claims.Should().BeEmpty();
    }

    [Fact] // AUTH-32
    public void A_role_an_action_already_wrote_is_not_stated_twice()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimType, "Manager")],
            authenticationType: "test"));

        RoleClaims.AddTo(principal, Token("""{"permissions":["manager:role"]}"""), ClaimType);

        principal.FindAll(ClaimType).Should().HaveCount(1);
    }

    private static IReadOnlyList<string> Roles(string? token)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "test"));

        RoleClaims.AddTo(principal, token, ClaimType);

        return principal.FindAll(ClaimType).Select(claim => claim.Value).ToArray();
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
