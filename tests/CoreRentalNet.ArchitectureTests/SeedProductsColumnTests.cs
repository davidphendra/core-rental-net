using System.Text.RegularExpressions;
using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using CoreRentalNet.Modules.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreRentalNet.ArchitectureTests;

/// <summary>
/// The generated seed migration inserts exactly the columns the store maps.
/// </summary>
/// <remarks>
/// The populate tool renders the column list itself, so this is what keeps it in step with the model: a
/// column added, renamed or removed on the entity that the seed does not carry fails here rather than when
/// the migration is applied to a database.
/// </remarks>
public sealed partial class SeedProductsColumnTests
{
    [Fact] // CAT-15
    public void The_seed_migration_inserts_the_columns_the_store_maps()
    {
        var declared = DeclaredColumns();
        var mapped = MappedColumns();

        mapped.Should().NotBeEmpty("otherwise the comparison proves nothing");
        declared.Should().BeEquivalentTo(mapped);
    }

    private static string[] DeclaredColumns()
    {
        var block = ColumnsBlock().Match(MigrationText()).Groups["columns"].Value;

        return [.. ColumnName().Matches(block).Select(match => match.Groups["name"].Value)];
    }

    private static string[] MappedColumns()
    {
        var options = new DbContextOptionsBuilder<CatalogContext>();
        CatalogPersistence.Configure(options, new SqliteDatabaseSettings("design-time.db"));

        using var context = new CatalogContext(options.Options);

        var product = context.Model.GetEntityTypes().Single(entity => entity.GetTableName() == "Catalog_Product");

        return [.. product.GetProperties().Select(property => property.GetColumnName())];
    }

    private static string MigrationText()
        => File.ReadAllText(Directory
            .GetFiles(
                RepoRoot.Combine("src", "Modules", "Catalog", "CoreRentalNet.Modules.Catalog.Infrastructure", "Migrations"),
                "*_SeedProducts.cs")
            .Single());

    [GeneratedRegex(@"columns: new\[\]\s*\{(?<columns>[^}]*)\}", RegexOptions.Singleline)]
    private static partial Regex ColumnsBlock();

    [GeneratedRegex("\"(?<name>[A-Za-z]+)\"")]
    private static partial Regex ColumnName();
}
