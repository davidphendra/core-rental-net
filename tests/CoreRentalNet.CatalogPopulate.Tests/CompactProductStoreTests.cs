using Xunit;
using AwesomeAssertions;
using CoreRentalNet.CatalogPopulate.Compact;

namespace CoreRentalNet.CatalogPopulate.Tests;

public class CompactProductStoreTests
{
    [Fact]
    public void A_product_survives_a_round_trip()
    {
        var path = Path.Combine(Path.GetTempPath(), $"compact-{Guid.NewGuid():N}.jsonl");
        var meta = new CompactMeta(
            "B000000001", "Title", ["d"], ["Home & Kitchen", "Furniture", "Bean Bags"],
            new Dictionary<string, System.Text.Json.JsonElement>(), 10.0, 4.0, 3, "Store", "https://example.com/i.jpg");
        var product = new CompactProduct(
            "B000000001", "Beanbag", meta, [new CompactReview(5.0, "Great", "Love it", 2, 123)], 1);

        try
        {
            CompactProductStore.Write(path, [product]);
            var read = CompactProductStore.Read(path).Single();

            read.ParentAsin.Should().Be("B000000001");
            read.CategoryKey.Should().Be("Beanbag");
            read.ReviewCount.Should().Be(1);
            read.Reviews.Single().Text.Should().Be("Love it");
            read.Meta.Title.Should().Be("Title");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void A_missing_compact_file_says_how_to_make_one()
    {
        var act = () => CompactProductStore.Read("does-not-exist.jsonl").ToList();

        act.Should().Throw<FileNotFoundException>().WithMessage("*--extract*");
    }
}