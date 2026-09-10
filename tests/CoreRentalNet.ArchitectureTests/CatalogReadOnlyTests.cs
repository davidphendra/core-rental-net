using System.Reflection;
using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Domain;
using Xunit;

namespace CoreRentalNet.ArchitectureTests;

public sealed class CatalogReadOnlyTests
{
    private static readonly string[] MutationVerbs =
    [
        "Add", "Create", "Update", "Delete", "Remove", "Insert", "Upsert", "Save", "Set", "Put", "Patch", "Replace",
    ];

    [Fact] // CAT-13
    public void The_catalog_module_exposes_no_way_to_change_a_product()
    {
        var offenders = CatalogAssemblies()
            .SelectMany(assembly => assembly.GetExportedTypes())
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .Select(method => (Type: type, Method: method)))
            .Where(entry => MutationVerbs.Any(verb =>
                entry.Method.Name.StartsWith(verb, StringComparison.Ordinal)
                && entry.Method.Name.Length > verb.Length
                && char.IsUpper(entry.Method.Name[verb.Length])))
            .Select(entry => $"{entry.Type.FullName}.{entry.Method.Name}")
            .ToArray();

        offenders.Should().BeEmpty(
            "the catalog is a read-only source: no add, update or delete exists anywhere (ADR-0005)");
    }

    [Fact] // CAT-13
    public void The_catalog_has_no_persistence_layer()
    {
        var catalogTypes = CatalogAssemblies().SelectMany(assembly => assembly.GetExportedTypes()).ToArray();

        catalogTypes.Should().NotContain(
            type => HasEfCoreBaseType(type),
            "Catalog is an in-memory snapshot with no database (ADR-0005)");
    }

    private static IEnumerable<Assembly> CatalogAssemblies() =>
    [
        typeof(Product).Assembly,
        typeof(CoreRentalNet.Modules.Catalog.Application.Contracts.IDefineProductPrices).Assembly,
        typeof(CoreRentalNet.Modules.Catalog.Infrastructure.Loading.ProductCatalogSnapshot).Assembly,
    ];

    /// <summary>Checked by name so this project needs no reference to EF Core.</summary>
    private static bool HasEfCoreBaseType(Type type)
    {
        for (var candidate = type.BaseType; candidate is not null; candidate = candidate.BaseType)
        {
            if (candidate.FullName?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true)
            {
                return true;
            }
        }

        return false;
    }
}
