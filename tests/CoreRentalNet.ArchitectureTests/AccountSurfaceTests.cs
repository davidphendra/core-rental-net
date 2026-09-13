using AwesomeAssertions;
using Xunit;

namespace CoreRentalNet.ArchitectureTests;

/// <summary>
/// The account surface: the menu in the header, and the profile page it points at. Source-level,
/// because the contract is between two files rather than between two types, and the compiler cannot
/// see an <c>href</c>.
/// </summary>
public sealed class AccountSurfaceTests
{
    private static string Source(params string[] parts)
        => File.ReadAllText(RepoRoot.Combine(["src", "Host", "CoreRentalNet.Host", .. parts]));

    [Fact] // AUTH-27
    public void The_account_menu_sends_a_signed_in_customer_to_the_profile_page()
    {
        // The menu names the page's own constant rather than repeating the address, so the two can
        // only drift if the page's route stops matching its constant - which is checked here.
        Source("Components", "Shared", "AccountMenu.razor")
            .Should().Contain("href=\"@Profile.Path\"");

        var profile = Source("Components", "Pages", "Profile.razor");

        profile.Should().Contain("@page \"/profile\"");
        profile.Should().Contain("public const string Path = \"/profile\"");
    }

    [Fact] // AUTH-27
    public void The_account_menu_offers_a_way_out()
    {
        // The same single address every other sign-out affordance uses, so the menu, the refused
        // page and the account endpoint cannot disagree about where signing out is.
        Source("Components", "Shared", "AccountMenu.razor")
            .Should().Contain("@AccountController.SignOutPath");
    }

    [Fact] // AUTH-28
    public void The_profile_page_shows_the_name_email_and_role_from_the_snapshot()
    {
        var profile = Source("Components", "Pages", "Profile.razor");

        profile.Should().Contain("Customer.Name");
        profile.Should().Contain("Customer.Email");
        profile.Should().Contain("Customer.RoleLabel");
    }

    [Fact] // AUTH-26
    public void The_profile_page_states_a_missing_role_instead_of_drawing_one()
    {
        // A role is the provider's answer, delivered only when the tenant's Action emits it. An
        // empty line is left blank only where the page says so, and a chip is drawn only when there
        // is a role to put in it.
        var profile = Source("Components", "Pages", "Profile.razor");

        profile.Should().Contain("string.IsNullOrWhiteSpace(Customer.RoleLabel)");
    }
}
