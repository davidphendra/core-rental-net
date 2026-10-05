using AwesomeAssertions;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Domain;
using CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;
using NetArchTest.Rules;
using Xunit;

namespace CoreRentalNet.ArchitectureTests;

/// <summary>
/// The Adapter boundary: the provider is named in infrastructure and nowhere else.
/// </summary>
/// <remarks>
/// The reason the suggestion run was moved into the module at all. If the application or the domain ever names
/// Azure, OpenAI or the agent SDK, the whole point of the port - that a provider can be replaced without the
/// use case noticing - is gone, and this is the rule that says so.
/// </remarks>
public sealed class ProviderIsolationTests
{
    private static readonly string[] ProviderNamespaces =
        ["Microsoft.Agents.AI", "Azure.AI.Projects", "Azure.Identity", "OpenAI", "Microsoft.Extensions.AI"];

    [Fact]
    public void The_application_and_domain_name_no_provider()
    {
        var assemblies = new[] { typeof(IWorkspaceSuggestionAgentAdapter).Assembly, typeof(WorkspaceSuggestionVerdict).Assembly };

        foreach (var assembly in assemblies)
        {
            var result = Types.InAssembly(assembly)
                .That().ResideInNamespaceStartingWith("CoreRentalNet")
                .ShouldNot().HaveDependencyOnAny(ProviderNamespaces)
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"{assembly.GetName().Name} must reach the agent only through its port. Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
    }

    [Fact] // and the rule above is not vacuous: the adapter still names them
    public void The_adapter_still_names_them()
    {
        var infrastructure = typeof(AgentFoundryWorkspaceSuggestionAdapter).Assembly;

        // The factory is the one type that resolves the provider's client and credential, so it is the right
        // thing to check: if it stopped naming a provider, the port would have nothing real behind it.
        Types.InAssembly(infrastructure)
            .That().HaveName("AgentFoundryWorkspaceSuggestionAdapterFactory")
            .GetTypes()
            .Should().NotBeEmpty("otherwise the rule below passes because it selected nothing");

        var result = Types.InAssembly(infrastructure)
            .That().HaveName("AgentFoundryWorkspaceSuggestionAdapterFactory")
            .Should().HaveDependencyOnAny(ProviderNamespaces)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "the adapter is where the provider is named, so it must still name one");
    }
}
