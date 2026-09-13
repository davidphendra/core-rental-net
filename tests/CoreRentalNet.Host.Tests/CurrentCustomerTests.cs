using System.Security.Claims;
using AwesomeAssertions;
using CoreRentalNet.Host.Presentation;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The snapshot of who is signed in, as the profile page and the account menu read it.
/// </summary>
/// <remarks>
/// The claims are the provider's, not this application's: Auth0 issues the name under the OIDC
/// <c>profile</c> scope, and it may fill <c>name</c> with the email for an account that signed up
/// with an address and nothing else. The role arrives only from the tenant's Action, under a
/// namespaced claim. None of those is an error, so each is pinned here rather than left to be
/// discovered on a tenant that maps its claims differently.
/// </remarks>
public sealed class CurrentCustomerTests
{
    private static ClaimsPrincipal SignedIn(params Claim[] claims)
        => new(new ClaimsIdentity(claims, authenticationType: "test"));

    [Fact] // AUTH-25
    public void An_unauthenticated_request_is_anonymous()
        => CurrentCustomer.FromClaims(new ClaimsPrincipal(new ClaimsIdentity()))
            .Should().Be(CurrentCustomer.Anonymous);

    [Fact] // AUTH-25
    public void A_null_principal_is_anonymous()
        => CurrentCustomer.FromClaims(null).Should().Be(CurrentCustomer.Anonymous);

    [Fact] // AUTH-25
    public void The_standard_claims_fill_the_snapshot()
    {
        var customer = CurrentCustomer.FromClaims(SignedIn(
            new Claim(ClaimTypes.NameIdentifier, "auth0|42"),
            new Claim(ClaimTypes.Email, "moni@example.test"),
            new Claim(ClaimTypes.Name, "Moni S")));

        customer.Subject.Should().Be("auth0|42");
        customer.Email.Should().Be("moni@example.test");
        customer.Name.Should().Be("Moni S");
        customer.IsSignedIn.Should().BeTrue();
        customer.DisplayName.Should().Be("Moni S");
        customer.Initials.Should().Be("MS");
    }

    [Fact] // AUTH-25
    public void The_short_oidc_names_are_read_when_the_standard_ones_are_absent()
    {
        var customer = CurrentCustomer.FromClaims(SignedIn(
            new Claim("sub", "auth0|7"),
            new Claim("email", "moni@example.test"),
            new Claim("nickname", "Moni")));

        customer.Subject.Should().Be("auth0|7");
        customer.Email.Should().Be("moni@example.test");
        customer.Name.Should().Be("Moni");
    }

    [Fact] // AUTH-25
    public void The_name_parts_are_joined_when_the_provider_sends_them()
        => CurrentCustomer.FromClaims(SignedIn(
                new Claim(ClaimTypes.NameIdentifier, "auth0|42"),
                new Claim("given_name", "Moni"),
                new Claim("family_name", "Sukarta")))
            .Name.Should().Be("Moni Sukarta");

    [Fact] // AUTH-25 — the reported bug: Auth0 fills `name` with the email for an email-only signup
    public void An_email_in_the_name_claim_gives_way_to_the_name_parts()
        => CurrentCustomer.FromClaims(SignedIn(
                new Claim(ClaimTypes.NameIdentifier, "auth0|42"),
                new Claim("name", "moni@example.test"),
                new Claim("given_name", "Moni"),
                new Claim("family_name", "Sukarta")))
            .Name.Should().Be("Moni Sukarta");

    [Fact] // AUTH-25
    public void An_email_in_the_name_claim_gives_way_to_a_nickname_that_is_not_one()
        => CurrentCustomer.FromClaims(SignedIn(
                new Claim(ClaimTypes.NameIdentifier, "auth0|42"),
                new Claim("name", "moni@example.test"),
                new Claim("nickname", "Moni")))
            .Name.Should().Be("Moni");

    [Fact] // AUTH-25
    public void An_account_with_only_an_email_has_no_name()
    {
        var customer = CurrentCustomer.FromClaims(SignedIn(
            new Claim(ClaimTypes.NameIdentifier, "auth0|42"),
            new Claim("name", "moni@example.test"),
            new Claim("email", "moni@example.test"),
            new Claim("nickname", "moni@example.test")));

        customer.Name.Should().BeNull();
        customer.Email.Should().Be("moni@example.test");
    }

    [Fact] // AUTH-25
    public void An_email_with_no_name_is_shown_as_the_email()
    {
        var customer = CurrentCustomer.FromClaims(SignedIn(
            new Claim(ClaimTypes.NameIdentifier, "auth0|42"),
            new Claim(ClaimTypes.Email, "moni@example.test")));

        customer.Name.Should().BeNull();
        customer.DisplayName.Should().Be("moni@example.test");
        customer.Initials.Should().Be("ME");
    }

    [Fact] // AUTH-26
    public void The_role_claim_the_tenant_was_told_to_emit_is_read()
        => CurrentCustomer.FromClaims(
                SignedIn(
                    new Claim(ClaimTypes.NameIdentifier, "auth0|42"),
                    new Claim("https://corerental/roles", "Administrator")))
            .RoleLabel.Should().Be("Administrator");

    [Fact] // AUTH-26
    public void Every_role_in_the_namespaced_claim_is_kept()
        => CurrentCustomer.FromClaims(SignedIn(
                new Claim(ClaimTypes.NameIdentifier, "auth0|42"),
                new Claim("https://corerental/roles", "Customer"),
                new Claim("https://corerental/roles", "Beta Tester")))
            .RoleLabel.Should().Be("Customer, Beta Tester");

    [Fact] // AUTH-26
    public void A_role_claim_under_another_namespace_is_still_read()
        => CurrentCustomer.FromClaims(SignedIn(
                new Claim(ClaimTypes.NameIdentifier, "auth0|42"),
                new Claim("https://contoso.example/roles", "Administrator")))
            .RoleLabel.Should().Be("Administrator");

    [Fact] // AUTH-26
    public void A_claim_named_quite_differently_is_read_when_configuration_names_it()
        => CurrentCustomer.FromClaims(
                SignedIn(
                    new Claim(ClaimTypes.NameIdentifier, "auth0|42"),
                    new Claim("https://contoso.example/roleNames", "Administrator")),
                "https://contoso.example/roleNames")
            .RoleLabel.Should().Be("Administrator");

    [Fact] // AUTH-26
    public void The_standard_role_claim_still_answers_for_another_provider()
        => CurrentCustomer.FromClaims(SignedIn(
                new Claim(ClaimTypes.NameIdentifier, "auth0|42"),
                new Claim(ClaimTypes.Role, "Administrator")))
            .RoleLabel.Should().Be("Administrator");

    [Fact] // AUTH-26 — the reported bug: the role was hard-coded to `Customer`
    public void A_provider_that_issues_no_role_leaves_the_role_empty()
        => CurrentCustomer.FromClaims(SignedIn(new Claim(ClaimTypes.NameIdentifier, "auth0|42")))
            .RoleLabel.Should().BeNull();
}
