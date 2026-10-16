using AwesomeAssertions;
using CoreRentalNet.CatalogIngestion.Seeding;
using Xunit;

namespace CoreRentalNet.CatalogIngestion.Tests.Seeding;

/// <summary>What the generated seed migration says.</summary>
public sealed class SeedMigrationWriterTests
{
    [Fact]
    public void The_migration_seeds_the_recipe_and_the_vectors()
    {
        using var file = new TemporaryFile();

        SeedMigrationWriter.Write(file.Path, Seed(vectors: 2), new RecordingProgressReporter());

        var text = File.ReadAllText(file.Path);

        text.Should().Contain("partial class SeedProductEmbeddings");
        text.Should().Contain("migrationBuilder.InsertData");
        text.Should().Contain("table: \"Discovery_ProductVectorRecipe\"");
        text.Should().Contain("migrationBuilder.Sql");
        text.Should().Contain("INSERT INTO Discovery_ProductVector");
    }

    [Fact]
    public void The_recipe_carries_the_model_the_width_and_the_composition()
    {
        using var file = new TemporaryFile();

        SeedMigrationWriter.Write(file.Path, Seed(vectors: 1), new RecordingProgressReporter());

        File.ReadAllText(file.Path).Should().Contain(
            "\"text-embedding-3-small\", 384, \"name+description+metadata/1\"");
    }

    [Fact]
    public void A_name_with_a_quote_is_escaped_for_sql()
    {
        using var file = new TemporaryFile();

        SeedMigrationWriter.Write(file.Path, Seed(vectors: 1, name: "It's a desk"), new RecordingProgressReporter());

        File.ReadAllText(file.Path).Should().Contain("'It''s a desk'");
    }

    [Fact]
    public void Rows_are_split_so_no_single_literal_is_disproportionate()
    {
        using var file = new TemporaryFile();

        SeedMigrationWriter.Write(file.Path, Seed(vectors: 150), new RecordingProgressReporter());

        Count(File.ReadAllText(file.Path), "INSERT INTO Discovery_ProductVector (skuNo").Should().Be(
            2,
            "150 rows are written in batches of 100");
    }

    [Fact]
    public void The_rows_are_written_in_the_order_they_came()
    {
        using var file = new TemporaryFile();

        SeedMigrationWriter.Write(file.Path, Seed(vectors: 3), new RecordingProgressReporter());

        var text = File.ReadAllText(file.Path);

        text.IndexOf("'SKU0000'", StringComparison.Ordinal).Should().BeLessThan(
            text.IndexOf("'SKU0001'", StringComparison.Ordinal));
        text.IndexOf("'SKU0001'", StringComparison.Ordinal).Should().BeLessThan(
            text.IndexOf("'SKU0002'", StringComparison.Ordinal));
    }

    [Fact]
    public void The_write_reports_every_batch_through_to_completion()
    {
        using var file = new TemporaryFile();
        var reporter = new RecordingProgressReporter();

        SeedMigrationWriter.Write(file.Path, Seed(vectors: 150), reporter);

        reporter.Steps().Should().ContainSingle().Which.Should().Be("Writing migration");
        reporter.Last("Writing migration").Total.Should().Be(2, "150 rows are written in batches of 100");
        reporter.Last("Writing migration").Completed.Should().Be(2);
    }

    private static int Count(string text, string needle)
    {
        var count = 0;

        for (var index = text.IndexOf(needle, StringComparison.Ordinal);
             index >= 0;
             index = text.IndexOf(needle, index + needle.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }

    private static EmbeddingSeed Seed(int vectors, string name = "A desk")
        => new(
            new SeedRecipe(
                "text-embedding-3-small",
                384,
                "name+description+metadata/1",
                "7f50",
                "2026-10-06T03:32:00.6567730+00:00"),
            [
                .. Enumerable.Range(0, vectors).Select(index => new SeedVector(
                    $"SKU{index:D4}",
                    "0040C7BA",
                    $"id-{index}",
                    name,
                    "A description.",
                    "2026-10-06T03:32:00.6567730+00:00")),
            ]);
}
