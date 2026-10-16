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
            "the catalogService is a read-only source: no add, update or delete exists anywhere");
    }

    [Fact] // CAT-13
    public void The_catalog_persistence_is_not_published_to_callers()
    {
        // There is a store now, and the rule is no longer "there is none" but "callers cannot reach it":
        // the entity set and every other EF type stay internal, and the published surface is the port.
        var offenders = CatalogAssemblies()
            .SelectMany(assembly => assembly.GetExportedTypes())
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(property => property.PropertyType.FullName?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true)
            .Select(property => $"{property.DeclaringType?.FullName}.{property.Name}")
            .ToArray();

        offenders.Should().BeEmpty("no catalogue table is reachable from a published type");
    }

    [Fact] // CAT-13
    public void The_rule_is_about_a_catalog_module_that_exists()
    {
        // NON-VACUITY. Both rules above pass on an empty assembly, so what they are about is asserted
        // here rather than assumed.
        var types = CatalogAssemblies().SelectMany(assembly => assembly.GetExportedTypes()).ToArray();

        types.Should().Contain(typeof(Product), "otherwise the mutation rule has no type to inspect");
        types.Should().Contain(typeof(IProductCatalogService));
        types.Should().Contain(typeof(SqliteProductCatalogService), "otherwise the persistence rule has no type to inspect");
        types.Should().Contain(typeof(CatalogContext), "otherwise there is no persistence surface to police");

        var methods = types
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .ToArray();

        methods.Should().NotBeEmpty("otherwise the mutation rule inspects no method");
    }

    private static IEnumerable<Assembly> CatalogAssemblies() =>
    [
        typeof(Product).Assembly,
        typeof(IProductCatalogService).Assembly,
        typeof(SqliteProductCatalogService).Assembly,
    ];
}
