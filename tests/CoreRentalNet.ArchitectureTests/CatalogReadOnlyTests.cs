using System.Reflection;
using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Domain;
using CoreRentalNet.Modules.Catalog.Infrastructure;
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
            "the catalog is a read-only source: no add, update or delete exists anywhere");
    }

    [Fact] // CAT-13
    public void The_catalog_has_no_persistence_layer()
    {
        var catalogTypes = CatalogAssemblies().SelectMany(assembly => assembly.GetExportedTypes()).ToArray();

        catalogTypes.Should().NotContain(
            type => HasEfCoreBaseType(type),
            "Catalog is an in-memory snapshot with no database");
    }

    [Fact] // CAT-13
    public void The_rule_is_about_a_catalog_module_that_exists()
    {
        // NON-VACUITY, AND IT NOW MATTERS MORE THAN IT DID. Both rules above pass on an empty assembly, and
        // e06 added a second module that owns a vector table and a mutable selection signal ON PURPOSE - so the
        // pressure to give Catalog a persistence layer too is now real, and the tempting way to make these
        // rules stop complaining is to loosen them. What they are about is asserted here rather than assumed,
        // so a rule that has stopped scanning anything fails instead of passing quietly.
        var types = CatalogAssemblies().SelectMany(assembly => assembly.GetExportedTypes()).ToArray();

        types.Should().Contain(typeof(Product), "otherwise the mutation rule has no type to inspect");
        types.Should().Contain(typeof(IProductCatalog));
        types.Should().Contain(typeof(ProductCatalog), "otherwise the persistence rule has no type to inspect");

        var methods = types
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .ToArray();

        methods.Should().NotBeEmpty("otherwise the mutation rule inspects no method");
    }

    private static IEnumerable<Assembly> CatalogAssemblies() =>
    [
        typeof(Product).Assembly,
        typeof(IProductCatalog).Assembly,
        typeof(ProductCatalog).Assembly,
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
