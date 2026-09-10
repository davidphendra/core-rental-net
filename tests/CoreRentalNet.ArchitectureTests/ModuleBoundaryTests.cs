using System.Reflection;
using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using NetArchTest.Rules;
using Xunit;

namespace CoreRentalNet.ArchitectureTests;

public sealed class ModuleBoundaryTests
{
    [Fact] // ARC-01
    public void No_module_depends_on_another_modules_domain_or_infrastructure()
    {
        var modules = ModuleAssemblies();

        // Guards against the rule silently going vacuous: if the repository has N module
        // directories, N modules must have been discovered in the test output.
        var moduleDirectories = Directory.GetDirectories(RepoRoot.Combine("src", "Modules")).Length;
        modules.Select(entry => entry.Module).Distinct().Count().Should().BeGreaterThanOrEqualTo(
            moduleDirectories,
            "every module on disk must be discovered, otherwise this rule proves nothing");

        foreach (var (module, assembly) in modules)
        {
            var foreign = modules
                .Where(candidate => !string.Equals(candidate.Module, module, StringComparison.Ordinal))
                .SelectMany(candidate => new[]
                {
                    $"CoreRentalNet.Modules.{candidate.Module}.Domain",
                    $"CoreRentalNet.Modules.{candidate.Module}.Infrastructure",
                })
                .ToArray();

            var result = Types.InAssembly(assembly)
                .That().ResideInNamespaceStartingWith($"CoreRentalNet.Modules.{module}")
                .ShouldNot().HaveDependencyOnAny(foreign)
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"{assembly.GetName().Name} must reach other modules only through their Application contracts. Failing types: {Describe(result.FailingTypeNames)}");
        }
    }

    [Fact] // ARC-04
    public void Domain_assemblies_do_not_depend_on_ef_core_or_asp_net()
    {
        var forbidden = new[] { "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Microsoft.Data.Sqlite" };

        foreach (var assembly in DomainAssemblies())
        {
            var result = Types.InAssembly(assembly)
                .That().ResideInNamespaceStartingWith("CoreRentalNet")
                .ShouldNot().HaveDependencyOnAny(forbidden)
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"{assembly.GetName().Name} is a Domain assembly and must not depend on EF Core or ASP.NET. Failing types: {Describe(result.FailingTypeNames)}");
        }
    }

    [Fact] // ARC-01
    public void Only_the_catalog_application_contracts_are_reachable_from_outside_the_module()
    {
        var catalogDomain = typeof(CoreRentalNet.Modules.Catalog.Domain.Product).Assembly;
        var catalogApplication = typeof(CoreRentalNet.Modules.Catalog.Application.Contracts.IDefineProductPrices).Assembly;

        catalogDomain.Should().NotBeSameAs(catalogApplication);

        Types.InAssembly(catalogApplication)
            .That().ResideInNamespace("CoreRentalNet.Modules.Catalog.Application.Contracts")
            .Should().BePublic()
            .GetResult().IsSuccessful.Should().BeTrue();
    }

    private static IReadOnlyList<(string Module, Assembly Assembly)> ModuleAssemblies()
        => Directory
            .GetFiles(AppContext.BaseDirectory, "CoreRentalNet.Modules.*.dll")
            .Where(path => !path.EndsWith(".resources.dll", StringComparison.Ordinal))
            .Select(Assembly.LoadFrom)
            .Select(assembly => (Module: ModuleOf(assembly), Assembly: assembly))
            .Where(entry => entry.Module is not null)
            .Select(entry => (entry.Module!, entry.Assembly))
            .GroupBy(entry => entry.Item1)
            .Select(group => group.First())
            .ToArray();

    private static string? ModuleOf(Assembly assembly)
    {
        var parts = assembly.GetName().Name?.Split('.');

        return parts is { Length: >= 4 } && parts[0] == "CoreRentalNet" && parts[1] == "Modules"
            ? parts[2]
            : null;
    }

    private static IEnumerable<Assembly> DomainAssemblies()
        => new[] { typeof(Money).Assembly, typeof(CoreRentalNet.Modules.Catalog.Domain.Product).Assembly };

    private static string Describe(IEnumerable<string>? failingTypes)
        => failingTypes is null ? "(none reported)" : string.Join(", ", failingTypes);
}
