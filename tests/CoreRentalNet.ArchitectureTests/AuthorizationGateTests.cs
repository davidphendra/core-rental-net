using AwesomeAssertions;
using Xunit;

namespace CoreRentalNet.ArchitectureTests;

/// <summary>
/// Source-level checks on the gate in front of the builder, because a gate is only a gate where it is
/// enforced, and this one has to be enforced in two places.
/// </summary>
public sealed class AuthorizationGateTests
{
    private static string Source(params string[] parts)
        => File.ReadAllText(RepoRoot.Combine(["src", "Host", "CoreRentalNet.Host", .. parts]));

    [Fact] // AUTH-13
    public void The_builder_asks_for_the_catalog_policy()
    {
        Source("Components", "Pages", "Builder.razor")
            .Should().Contain("[Authorize(Policy = CatalogPolicy.Name)]");
    }

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

    [Fact] // AUTH-15
    public void Nothing_but_the_policy_says_what_entitles_a_reader()
    {
        // The claim is configuration, so no claim type or value is written into the application.
        var host = Path.Combine(RepoRoot.Path, "src", "Host");

        var writtenIntoCode = Directory
            .GetFiles(host, "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(host, "*.razor", SearchOption.AllDirectories))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(file => File.ReadAllText(file).Contains("read:catalog", StringComparison.Ordinal))
            .ToArray();

        writtenIntoCode.Should().BeEmpty("the claim belongs in appsettings, not in the code");
    }
}
