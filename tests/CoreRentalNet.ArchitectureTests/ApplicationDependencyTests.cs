using System.Reflection;
using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Rentals.Application.Orders;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Services;
using NetArchTest.Rules;
using Xunit;

namespace CoreRentalNet.ArchitectureTests;

/// <summary>
/// The Dependency Rule one layer further in than <see cref="ModuleBoundaryTests"/> goes: an
/// Application assembly is a use-case layer, so it must not name a storage or web framework either.
/// Domain is covered by ARC-04; this closes the same door for Application, which is where a
/// convenience call to EF Core would otherwise be easy to miss in review.
/// </summary>
public sealed class ApplicationDependencyTests
{
    private static readonly string[] Forbidden =
        ["Microsoft.EntityFrameworkCore", "Microsoft.Data.Sqlite", "Microsoft.AspNetCore"];

    [Fact] // ARC-04
    public void Application_assemblies_do_not_depend_on_a_storage_or_web_framework()
    {
        var assemblies = ApplicationAssemblies().ToArray();

        // One shared kernel plus each module's Application project, so the rule cannot pass by
        // discovering nothing.
        assemblies.Length.Should().BeGreaterThanOrEqualTo(
            4,
            "every Application assembly must be discovered, otherwise this rule proves nothing");

        foreach (var assembly in assemblies)
        {
            var result = Types.InAssembly(assembly)
                .That().ResideInNamespaceStartingWith("CoreRentalNet")
                .ShouldNot().HaveDependencyOnAny(Forbidden)
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"{assembly.GetName().Name} is an Application assembly and must not depend on EF Core, SQLite or ASP.NET. Failing types: {Describe(result.FailingTypeNames)}");
        }
    }

    private static IEnumerable<Assembly> ApplicationAssemblies()
        =>
        [
            typeof(IMoneyService).Assembly,
            typeof(IProductCatalog).Assembly,
            typeof(IPlaceOrder).Assembly,
            typeof(IWorkspaceService).Assembly,
        ];

    private static string Describe(IEnumerable<string>? failingTypes)
        => failingTypes is null ? "(none reported)" : string.Join(", ", failingTypes);
}
