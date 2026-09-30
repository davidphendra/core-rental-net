using System.Text.RegularExpressions;
using AwesomeAssertions;
using CoreRentalNet.Host.Components.Shared.Suggestion;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;
using CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CoreRentalNet.Host.Tests.WorkspaceSuggestion;

/// <summary>
/// Every service a component injects is in the container, so a panel cannot throw the moment it renders.
/// </summary>
/// <remarks>
/// <para>
/// The failure this exists for is silent until it is a page: a component that injects a service nobody
/// registered compiles, passes every unit test, and throws
/// <c>InvalidOperationException: Cannot provide a value for property …</c> on the first render. That is exactly
/// how the notice registry shipped unregistered.
/// </para>
/// <para>
/// It reads the markup for the injections, because that is where the dependency is written, and it resolves
/// each through the real container. Framework services - <c>IJSRuntime</c> and the rest, provided per circuit
/// rather than by this application - are not this rule's business, so only the application's own namespaces are
/// checked.
/// </para>
/// </remarks>
public sealed class SuggestionCompositionTests(SuggestionEndpointFactory factory)
    : IClassFixture<SuggestionEndpointFactory>
{
    [Fact]
    public void Every_service_a_component_injects_is_registered()
    {
        var injected = InjectedApplicationTypes();

        // Guards against the rule passing because it found nothing to check, and ties it to the failure it was
        // written for: the notice registry is injected by the list, so it must be among what was scanned.
        injected.Should().NotBeEmpty("the components inject the application, so there is a seam to check");
        injected.Should().Contain(
            typeof(ISuggestionNoticeComponentResolver),
            "the notice list injects the registry, which is exactly the injection that shipped unregistered");

        using var scope = factory.Services.CreateScope();

        var missing = injected
            .Where(type => scope.ServiceProvider.GetService(type) is null)
            .Select(type => type.FullName!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        missing.Should().BeEmpty(
            "a component that injects an unregistered service throws when it first renders, which no test tier below the browser sees");
    }

    /// <summary>The application-owned types the components inject, resolved by simple name.</summary>
    /// <remarks>
    /// Components write the <b>short</b> name - <c>@inject IWorkspaceSession Session</c> - so a type is the
    /// application's when that name is one this application declares, and a framework service when it is not.
    /// That is the distinction the rule needs, and it is why the markup alone cannot filter the two apart.
    /// </remarks>
    private static IReadOnlyList<Type> InjectedApplicationTypes()
    {
        var componentFiles = Directory.GetFiles(
            Path.Combine(RepositoryRoot(), "src", "Host", "CoreRentalNet.Host", "Components"),
            "*.razor",
            SearchOption.AllDirectories);

        var known = KnownTypes();

        return componentFiles
            .SelectMany(file => Regex
                .Matches(File.ReadAllText(file), @"@inject\s+([A-Za-z_][A-Za-z0-9_.]*)")
                .Select(match => match.Groups[1].Value))
            .Select(name => name.Split('.')[^1])
            .Distinct(StringComparer.Ordinal)
            .Where(name => known.ContainsKey(name))
            .Select(name => known[name])
            .ToArray();
    }

    /// <summary>The assemblies an injected application type can come from, keyed by simple name.</summary>
    private static Dictionary<string, Type> KnownTypes()
    {
        var assemblies = new[]
        {
            typeof(Program).Assembly,
            typeof(WorkspaceSuggestionAvailability).Assembly,
            typeof(MicrosoftFoundryAgentConnectionSettings).Assembly,
        };

        return assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true } or { IsInterface: true })
            .GroupBy(type => type.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CoreRentalNet.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root.");
    }
}
