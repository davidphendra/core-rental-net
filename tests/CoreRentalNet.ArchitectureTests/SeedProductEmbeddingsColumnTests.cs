using System.Text.RegularExpressions;
using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using CoreRentalNet.BuildingBlocks.Infrastructure.Vectors;
using CoreRentalNet.Modules.Discovery.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreRentalNet.ArchitectureTests;

/// <summary>
/// The generated embedding seed carries exactly the columns and width the store declares.
/// </summary>
/// <remarks>
/// <para>
/// <b>The generator renders the recipe's columns and the vectors' inserts itself, and the vec0 table is
/// created by hand, so nothing in the compiler ties the three together.</b> This is what does: a column, a
/// table name or a width changed in one place and not the others fails here rather than when a migration is
/// applied to a database.
/// </para>
/// <para>
/// <b>It reads the migration files as text, not the compiled context alone</b>, because a migration is a
/// frozen statement of a schema: the point is that what was written down still agrees with the model it was
/// generated from.
/// </para>
/// </remarks>
public sealed partial class SeedProductEmbeddingsColumnTests
{
    [Fact] // EMB-01
    public void The_recipe_seed_inserts_the_columns_the_store_maps()
    {
        var declared = ColumnsBlock(SeedText());

        var mapped = MappedRecipeColumns();

        mapped.Should().NotBeEmpty("otherwise the comparison proves nothing");
        declared.Should().BeEquivalentTo(mapped);
    }

    [Fact] // EMB-01
    public void The_vector_seed_inserts_the_columns_the_table_declares()
    {
        var declared = VectorColumns();

        declared.Should().NotBeEmpty("otherwise the comparison proves nothing");
        VectorSeedColumns().Should().BeEquivalentTo(declared);
    }

    [Fact] // EMB-01
    public void The_tables_carry_the_contract_names_and_the_vector_width_is_the_recipe_s()
    {
        RecipeTableName(SeedText()).Should().Be(ProductVectorContract.RecipeTable);
        VectorSeedTableName().Should().Be(ProductVectorContract.VectorTable);
        VectorTableName().Should().Be(ProductVectorContract.VectorTable);

        VectorWidth().Should().Be(RecipeWidth(), "a vector table wider or narrower than the recipe describes is unreadable");
    }

    private static string[] MappedRecipeColumns()
    {
        var options = new DbContextOptionsBuilder<EmbeddingContext>();
        EmbeddingPersistence.Configure(options, new SqliteDatabaseSettings("design-time.db"));

        using var context = new EmbeddingContext(options.Options);

        var recipe = context.Model.GetEntityTypes().Single(entity => entity.GetTableName() == ProductVectorContract.RecipeTable);

        return [.. recipe.GetProperties().Select(property => property.GetColumnName())];
    }

    /// <summary>The column names of the seed's <c>InsertData</c>, in the order it states them.</summary>
    private static string[] ColumnsBlock(string migrationText)
        => [.. QuotedName().Matches(Columns().Match(migrationText).Groups["columns"].Value)
            .Select(match => match.Groups["name"].Value)];

    /// <summary>The column names of the vector seed's <c>INSERT</c> statement.</summary>
    private static string[] VectorSeedColumns()
        => [.. VectorInsert().Match(SeedText()).Groups["columns"].Value
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];

    /// <summary>The declared columns of the vec0 table, without the auxiliary <c>+</c> marker or the types.</summary>
    private static string[] VectorColumns()
        => [.. Vec0Body().Match(InitialCreateText()).Groups["body"].Value
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(line => Vec0Column().Match(line).Groups["name"].Value)
            .Where(name => name.Length > 0)];

    private static int VectorWidth()
        => int.Parse(VectorWidthSpec().Match(InitialCreateText()).Groups["width"].Value, System.Globalization.CultureInfo.InvariantCulture);

    private static int RecipeWidth()
        => int.Parse(RecipeValues().Match(SeedText()).Groups["width"].Value, System.Globalization.CultureInfo.InvariantCulture);

    private static string RecipeTableName(string migrationText)
        => InsertDataTable().Match(migrationText).Groups["table"].Value;

    private static string VectorSeedTableName()
        => VectorInsert().Match(SeedText()).Groups["table"].Value;

    private static string VectorTableName()
        => CreateVirtualTable().Match(InitialCreateText()).Groups["table"].Value;

    private static string SeedText() => MigrationText("*_SeedProductEmbeddings.cs");

    private static string InitialCreateText() => MigrationText("*_InitialCreate.cs");

    private static string MigrationText(string pattern)
        => File.ReadAllText(Directory
            .GetFiles(
                RepoRoot.Combine("src", "Modules", "Discovery", "CoreRentalNet.Modules.Discovery.Infrastructure", "Migrations"),
                pattern)
            .Single());

    [GeneratedRegex(@"columns: new\[\]\s*\{(?<columns>[^}]*)\}", RegexOptions.Singleline)]
    private static partial Regex Columns();

    [GeneratedRegex("\"(?<name>[A-Za-z_]+)\"")]
    private static partial Regex QuotedName();

    [GeneratedRegex(@"InsertData\(\s*table: ""(?<table>[^""]+)""")]
    private static partial Regex InsertDataTable();

    [GeneratedRegex(@"INSERT INTO (?<table>\w+) \((?<columns>[^)]+)\)")]
    private static partial Regex VectorInsert();

    [GeneratedRegex(@"CREATE VIRTUAL TABLE (?<table>\w+) USING vec0\((?<body>.*?)\);", RegexOptions.Singleline)]
    private static partial Regex CreateVirtualTable();

    [GeneratedRegex(@"USING vec0\((?<body>.*?)\);", RegexOptions.Singleline)]
    private static partial Regex Vec0Body();

    [GeneratedRegex(@"^\+?(?<name>\w+)", RegexOptions.Multiline)]
    private static partial Regex Vec0Column();

    [GeneratedRegex(@"float\[(?<width>\d+)\]")]
    private static partial Regex VectorWidthSpec();

    [GeneratedRegex(@"values: new object\[\] \{\s*""[^""]*"",\s*(?<width>\d+),")]
    private static partial Regex RecipeValues();
}
