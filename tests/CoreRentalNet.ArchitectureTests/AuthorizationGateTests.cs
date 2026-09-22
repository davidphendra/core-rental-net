using System.Text.RegularExpressions;
using AwesomeAssertions;
using Xunit;

namespace CoreRentalNet.ArchitectureTests;

/// <summary>
/// Source-level checks on the gate in front of the API, because a gate is only a gate where it is enforced.
/// </summary>
public sealed class AuthorizationGateTests
{
    private static string Source(params string[] parts)
        => File.ReadAllText(RepoRoot.Combine(["src", "Host", "CoreRentalNet.Host", .. parts]));

    [Fact] // AUTH-14
    public void The_router_enforces_it_too()
    {
        // A page attribute is checked when a request reaches the endpoint, and an interactive circuit
        // navigates without making one. Without the router the gate would hold on the way in and be
        // missing for every step taken inside.
        var routes = Source("Components", "Routes.razor");

        routes.Should().Contain("AuthorizeRouteView");
        routes.Should().Contain("CascadingAuthenticationState");
    }

    [Fact] // AUTH-16
    public void A_refused_account_is_sent_to_a_page_that_exists()
    {
        // The default for this is /Account/AccessDenied, which nothing here serves: measured, a
        // signed-in account without the claim landed on a blank 404 instead of being told anything.
        Source("Extentions", "IdentityRegistrationExtentions.cs")
            .Should().Contain("options.AccessDeniedPath = AccessDenied.Path");

        Source("Components", "Pages", "AccessDenied.razor")
            .Should().Contain("@page \"/access-denied\"");
    }

    [Fact] // AUTH-15
    public void Nothing_but_the_policy_says_what_entitles_a_reader()
    {
        // The claim is configuration, so no claim type or value is written into the application.
        var writtenIntoCode = HostSources()
            .Where(file => File.ReadAllText(file).Contains("read:catalog", StringComparison.Ordinal))
            .ToArray();

        writtenIntoCode.Should().BeEmpty("the claim belongs in appsettings, not in the code");
    }

    [Fact] // e05s06 security addendum: the new permission, and every claim value rather than one
    public void The_closed_permission_is_declared_closed_and_no_claim_value_is_written_into_code()
    {
        // The catalogue opens when nothing is configured; the similarity search must not, and "must not" is
        // declared where the permission is registered rather than left to whatever the handler defaults to.
        // Changing the answer to Open is a security decision, so it has to break this test to happen.
        Source("Extentions", "AuthorizationRegistrationExtentions.cs")
            .Should().Contain("UnconfiguredBehaviour.Closed");

        // AUTH-15 looks for the catalogue's own value. A permission added later would not be covered by
        // that, so this checks the shape instead: nothing in the application assigns a claim value.
        var assigned = HostSources()
            .Where(file => Regex.IsMatch(File.ReadAllText(file), @"ClaimValue\s*=\s*""[^""]+"""))
            .ToArray();

        assigned.Should().BeEmpty("which claim entitles a caller is configuration, never code");
    }

    /// <summary>Every C# and Razor file the application ships, without build output.</summary>
    private static IEnumerable<string> HostSources()
    {
        var host = Path.Combine(RepoRoot.Path, "src", "Host");

        return Directory.GetFiles(host, "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(host, "*.razor", SearchOption.AllDirectories))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));
    }
}
