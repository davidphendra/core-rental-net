using AwesomeAssertions;
using CoreRentalNet.Modules.Discovery.Application;
using CoreRentalNet.Modules.Discovery.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// The index's two tables, against a real SQLite file.
/// </summary>
/// <remarks>
/// These prove the storage, not the retrieval: that a vector comes back bit-for-bit, that it is stored as
/// binary rather than as text, that the recipe survives, and that a row nothing in the module could have
/// written is refused rather than quietly shortened. Whether the fourteen chosen products are the right
/// fourteen is a different question, and one this tier cannot answer — see the e06 test matrix.
/// </remarks>
public sealed class DiscoveryIndexTests
{
    private const string Sku = "DSKB08XN4JDR";

    private const int Width = 512;

    private static float[] AVector()
        => [.. Enumerable.Range(0, Width).Select(index => (index - (Width / 2f)) / Width)];

    [Fact]
    public async Task The_recipe_survives_a_round_trip_through_the_file()
    {
        await using var database = new SqliteTestDatabase();

        await using (var write = await database.CreateMigratedDiscoveryContextAsync())
        {
            write.Indexes.Add(new CatalogIndex
            {
                Name = "catalog",
                ModelId = "text-embedding-3-large",
                Width = Width,
                Composition = EmbeddedText.Composition,
                CatalogueHash = "0f1e2d3c4b5a6978",
            });

            await write.SaveChangesAsync();
        }

        await using var read = await database.CreateMigratedDiscoveryContextAsync();
        var recipe = await read.Indexes.SingleAsync();

        recipe.Name.Should().Be("catalog");
        recipe.ModelId.Should().Be("text-embedding-3-large");
        recipe.Width.Should().Be(Width);
        // Not a pin on the value - EmbeddedTextTests owns that. This asserts the round trip, and reads
        // the composition from the same constant the ingestion tool will, so there is no second copy.
        recipe.Composition.Should().Be(EmbeddedText.Composition);
        recipe.CatalogueHash.Should().Be("0f1e2d3c4b5a6978");
    }

    [Fact]
    public async Task A_vector_survives_the_round_trip_float_for_float()
    {
        await using var database = new SqliteTestDatabase();
        var written = AVector();

        await using (var write = await database.CreateMigratedDiscoveryContextAsync())
        {
            write.Vectors.Add(new CatalogVector { Sku = Sku, Embedding = written });

            await write.SaveChangesAsync();
        }

        await using var read = await database.CreateMigratedDiscoveryContextAsync();
        var vector = await read.Vectors.SingleAsync();

        vector.Sku.Should().Be(Sku);
        vector.Embedding.Should().Equal(written);
    }

    [Fact]
    public async Task A_vector_is_stored_as_binary_and_not_as_text()
    {
        await using var database = new SqliteTestDatabase();

        await using (var write = await database.CreateMigratedDiscoveryContextAsync())
        {
            write.Vectors.Add(new CatalogVector { Sku = Sku, Embedding = AVector() });

            await write.SaveChangesAsync();
        }

        await using var read = await database.CreateMigratedDiscoveryContextAsync();

        // Asked of the file rather than of the mapping: 512 floats are 2,048 bytes of binary, and the same
        // values written as JSON text would be roughly three times that. The number is the evidence that the
        // conversion is in force rather than the provider's default.
        var stored = await read.Database
            .SqlQuery<long>($"SELECT length(Embedding) AS \"Value\" FROM Discovery_CatalogVector WHERE Sku = {Sku}")
            .SingleAsync();

        stored.Should().Be(Width * sizeof(float));
    }

    [Fact]
    public async Task A_stored_vector_that_is_not_a_whole_number_of_floats_is_refused()
    {
        await using var database = new SqliteTestDatabase();
        await using var context = await database.CreateMigratedDiscoveryContextAsync();

        // Written with raw SQL because nothing in the module can produce such a row - which is exactly why
        // the reading side must refuse it. A truncated read would return a shorter vector that still scored,
        // and a product ranked on two thirds of its embedding is wrong rather than broken.
        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO Discovery_CatalogVector (Sku, Embedding) VALUES ({0}, {1})",
            Sku,
            new byte[] { 1, 2, 3 });

        var act = async () => await context.Vectors.ToListAsync();

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*whole number of*");
    }
}
