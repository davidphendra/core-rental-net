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
            // No exception: every module reaches every other module through its published
            // application contracts and nothing else. The vocabulary a caller needs is published
            // there, so a caller never has a reason to reach into another module's Domain.
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
        var assemblies = DomainAssemblies().ToArray();

        // One shared kernel plus the three modules, so the rule cannot pass by discovering nothing.
        assemblies.Length.Should().BeGreaterThanOrEqualTo(
            4,
            "every Domain assembly must be discovered, otherwise this rule proves nothing");

        foreach (var assembly in assemblies)
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
    public void The_catalog_application_contracts_are_public_and_complete()
    {
        var catalogDomain = typeof(CoreRentalNet.Modules.Catalog.Domain.Product).Assembly;
        var catalogContracts = typeof(CoreRentalNet.Modules.Catalog.Application.Contracts.IProductCatalog).Assembly;

        catalogDomain.Should().NotBeSameAs(catalogContracts);

        var contracts = Types.InAssembly(catalogContracts)
            .That().ResideInNamespace("CoreRentalNet.Modules.Catalog.Application.Contracts")
            .GetTypes()
            .ToArray();

        // Non-vacuity: the namespace holds the port, the view and the vocabulary. Without this
        // check the rule below would pass on an empty namespace.
        contracts.Should().Contain(typeof(CoreRentalNet.Modules.Catalog.Application.Contracts.IProductCatalog));
        contracts.Should().Contain(typeof(CoreRentalNet.Modules.Catalog.Application.Contracts.ProductView));
        contracts.Should().Contain(typeof(CoreRentalNet.Modules.Catalog.Application.Contracts.CatalogCategory));

        contracts.Should().OnlyContain(type => type.IsPublic, "a published contract is public");
    }

    [Fact] // ARC-01
    public void The_catalog_port_publishes_its_vocabulary_and_not_the_domain_types()
    {
        var port = typeof(CoreRentalNet.Modules.Catalog.Application.Contracts.IProductCatalog);

        var mentioned = port.GetMethods()
            .SelectMany(method => method.GetParameters()
                .Select(parameter => parameter.ParameterType)
                .Append(method.ReturnType))
            .SelectMany(type => type.IsGenericType ? type.GetGenericArguments() : [type])
            .ToArray();

        mentioned.Should().NotContain(
            typeof(CoreRentalNet.Modules.Catalog.Domain.Product),
            "a published signature must not name a Domain type");
        mentioned.Should().NotContain(typeof(CoreRentalNet.Modules.Catalog.Domain.ProductCategory));
        mentioned.Should().NotContain(typeof(CoreRentalNet.Modules.Catalog.Domain.ProductSubCategory));

        mentioned.Should().Contain(
            typeof(CoreRentalNet.Modules.Catalog.Application.Contracts.CatalogCategory),
            "otherwise this rule proves nothing");
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
        => Directory
            .GetFiles(AppContext.BaseDirectory, "CoreRentalNet.*.Domain.dll")
            .Where(path => !path.EndsWith(".resources.dll", StringComparison.Ordinal))
            .Select(Assembly.LoadFrom)
            .Append(typeof(Money).Assembly)
            .Distinct();

    private static string Describe(IEnumerable<string>? failingTypes)
        => failingTypes is null ? "(none reported)" : string.Join(", ", failingTypes);
}
