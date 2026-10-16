using AwesomeAssertions;
using CoreRentalNet.CatalogPopulate.Catalog;
using CoreRentalNet.CatalogPopulate.Writing;
using Xunit;

namespace CoreRentalNet.CatalogPopulate.Tests;

public sealed class SeedMigrationWriterTests
{
    [Fact]
    public void The_migration_inserts_one_row_per_product_in_catalogue_order()
    {
        using var file = new TemporaryFile();

        SeedMigrationWriter.Write(file.Path, [Record("CHA0001", "chair"), Record("DSK0001", "desk")]);

        var text = File.ReadAllText(file.Path);
        text.Should().Contain("migrationBuilder.InsertData");
        text.Should().Contain("migrationBuilder.DeleteData");
        text.IndexOf("\"CHA0001\"", StringComparison.Ordinal)
            .Should().BeLessThan(text.IndexOf("\"DSK0001\"", StringComparison.Ordinal));
    }

    [Fact]
    public void The_row_carries_the_mapped_category_and_subcategory()
    {
        using var file = new TemporaryFile();

        SeedMigrationWriter.Write(file.Path, [Record("LMP0001", "accessory", "lamp")]);

        File.ReadAllText(file.Path).Should().Contain("\"LMP0001\", \"Name LMP0001\", 3, 3,");
    }

    [Fact]
    public void The_metadata_is_written_as_json()
    {
        using var file = new TemporaryFile();

        SeedMigrationWriter.Write(file.Path, [Record("LMP0001", "accessory", "lamp")]);

        var text = File.ReadAllText(file.Path);
        text.Should().Contain("\"[\\\"lamp\\\"]\"");
        text.Should().Contain("\"{\\\"brand\\\":\\\"Acme\\\"}\"");
    }

    [Fact]
    public void A_description_with_quotes_is_escaped()
    {
        using var file = new TemporaryFile();

        SeedMigrationWriter.Write(file.Path, [Record("DSK0001", "desk", description: "A 60\" desk")]);

        File.ReadAllText(file.Path).Should().Contain("A 60\\\" desk");
    }

    private static ProductRecord Record(
        string sku,
        string category,
        string? subCategory = null,
        string description = "A thing.")
        => new()
        {
            SkuNo = sku,
            Name = $"Name {sku}",
            Category = category,
            SubCategory = subCategory,
            PricePerMonth = 100000,
            Currency = "IDR",
            Description = description,
            Image = "https://example.test/i.jpg",
            Metadata = new ProductMetadataRecord
            {
                Tags = [subCategory ?? category],
                Attributes = new Dictionary<string, string> { ["brand"] = "Acme" },
                BestFor = ["work"],
                NotFor = [],
            },
        };
}
