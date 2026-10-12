using Xunit;
using AwesomeAssertions;
using CoreRentalNet.CatalogPopulate.Catalog;
using CoreRentalNet.CatalogPopulate.Writing;

namespace CoreRentalNet.CatalogPopulate.Tests;

public class ProductCatalogWriterTests
{
    [Fact]
    public void The_written_file_is_indented_and_ends_with_a_newline()
    {
        var path = Path.Combine(Path.GetTempPath(), $"catalog-{Guid.NewGuid():N}.json");
        var record = new ProductRecord
        {
            SkuNo = "DSKB09DCYF7N",
            Name = "Tangkula Desk",
            Category = "desk",
            PricePerMonth = 160000,
            Currency = "IDR",
            Description = "A desk",
            SubCategory = null,
            Image = "https://example.com/i.jpg",
            Metadata = new ProductMetadataRecord
            {
                Tags = ["desk"],
                Attributes = new Dictionary<string, string> { ["brand"] = "Tangkula" },
                BestFor = ["work"],
                NotFor = [],
            },
        };

        try
        {
            ProductCatalogWriter.Write(path, [record]);
            var text = File.ReadAllText(path);

            text.Should().Contain("\n  {");
            text.Should().Contain("\"skuNo\": \"DSKB09DCYF7N\"");
            text.Should().EndWith("]\n");
        }
        finally
        {
            File.Delete(path);
        }
    }
}