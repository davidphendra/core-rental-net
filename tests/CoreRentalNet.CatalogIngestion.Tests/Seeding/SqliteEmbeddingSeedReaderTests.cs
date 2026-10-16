using AwesomeAssertions;
using CoreRentalNet.CatalogIngestion.Seeding;
using Xunit;

namespace CoreRentalNet.CatalogIngestion.Tests.Seeding;

/// <summary>Reading a seed out of a vector file.</summary>
public sealed class SqliteEmbeddingSeedReaderTests
{
    [Fact]
    public void It_reads_the_recipe_and_the_vectors_of_a_file_written_before_the_move()
    {
        using var database = new LegacyVectorFile();

        var seed = SqliteEmbeddingSeedReader.Read(database.Path);

        seed.Recipe.ModelId.Should().Be("text-embedding-3-small");
        seed.Recipe.Width.Should().Be(384);
        seed.Recipe.Composition.Should().Be("name+description+metadata/1");

        seed.Vectors.Should().HaveCount(1);
        seed.Vectors[0].SkuNo.Should().Be("DSK0001");
        seed.Vectors[0].EmbeddingHex.Should().Be(
            Convert.ToHexString(LegacyVectorFile.Embedding),
            "the stored bytes are carried rather than decoded and re-encoded");
    }
}
