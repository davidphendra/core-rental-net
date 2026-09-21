using AwesomeAssertions;
using CoreRentalNet.CatalogIngestion.Chunking;
using CoreRentalNet.CatalogIngestion.Embeddings;
using CoreRentalNet.CatalogIngestion.Tests.Embeddings;
using Xunit;

namespace CoreRentalNet.CatalogIngestion.Tests.Chunking;

/// <summary>
/// The semantic splitter, driven through a stand-in server.
/// </summary>
/// <remarks>
/// The stand-in's vectors mean nothing semantically, so nothing here says anything about the quality of the
/// boundaries. What it proves is the property the table depends on: a product's text yields vectors of the
/// configured width, the same way every time, and a short answer from the server is refused rather than
/// paired by position.
/// </remarks>
public sealed class SemanticProductChunkerTests
{
    private const int Width = 384;

    private static ChunkerSettings Settings() => new(256, 1, "Percentile", 95, null, 1, 200);

    private static SemanticProductChunker Chunker(StandInEmbeddingClient embeddings)
        => new(new ClientEmbeddingGenerator(embeddings), Settings());

    [Fact]
    public async Task A_product_text_becomes_at_least_one_vector_of_the_configured_width()
    {
        var vectors = await Chunker(new StandInEmbeddingClient(Width))
            .ChunkAsync(ProductText(), CancellationToken.None);

        vectors.Should().NotBeEmpty();
        vectors.Should().OnlyContain(vector => vector.Length == Width);
    }

    [Fact]
    public async Task The_same_text_splits_the_same_way_twice()
    {
        var chunker = Chunker(new StandInEmbeddingClient(Width));

        var first = await chunker.ChunkAsync(ProductText(), CancellationToken.None);
        var second = await chunker.ChunkAsync(ProductText(), CancellationToken.None);

        first.Should().HaveCount(second.Count);
    }

    [Fact]
    public async Task A_short_answer_from_the_server_is_refused()
    {
        // The library pairs each text with the embedding at the same position, so a short answer would draw
        // boundaries between the wrong sentences without failing anywhere else.
        var generator = new ClientEmbeddingGenerator(new StandInEmbeddingClient(Width, answerWith: 1));

        var act = async () => await generator.GenerateAsync(["one", "two", "three"]);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*short answer*");
    }

    private static string ProductText() =>
        "name: Desk DSK0001\n"
        + "description: A height-adjustable desk for a working space. It has a wide top and a metal frame.\n"
        + "tags: desk, standing\n"
        + "attribute type: sit-stand";
}
