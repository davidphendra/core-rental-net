using Xunit;
using System.Text.Json;
using AwesomeAssertions;
using CoreRentalNet.CatalogPopulate;
using CoreRentalNet.CatalogPopulate.Compact;
using CoreRentalNet.CatalogPopulate.Extracting;

namespace CoreRentalNet.CatalogPopulate.Tests;

public class ExtractorTests
{
    [Fact]
    public void Extract_matches_each_product_and_counts_its_reviews()
    {
        var directory = Directory.CreateTempSubdirectory("extract").FullName;
        try
        {
            var office = Meta(directory, "office.jsonl", Row("B000000001", "Standing Desk", "Office Products|Office Furniture & Lighting|Desks & Workstations"));
            var electronics = Meta(directory, "electronics.jsonl", Row("B000000002", "Monitor", "Electronics|Computers & Accessories|Monitors"));
            var tools = Meta(directory, "tools.jsonl", Row("B000000003", "Desk Lamp", "Tools & Home Improvement|Lighting & Ceiling Fans|Lamps"));
            var home = Meta(directory, "home.jsonl", Row("B000000004", "Bean Bag", "Home & Kitchen|Furniture|Bean Bags"));
            var reviews = Reviews(directory, "office-reviews.jsonl", Review("B000000001"), Review("B000000001"));
            var empty = Reviews(directory, "empty.jsonl");

            var compactPath = Path.Combine(directory, "compact.jsonl");
            var settings = new CatalogPopulateSettings(
                Mode.Extract, compactPath, compactPath, "products.json", "images",
                office, electronics, tools, home,
                reviews, empty, empty, empty,
                "http://localhost:8080/v1", "model", 20, 400);

            var reporter = new RecordingProgressReporter();

            new Extractor(settings).Run(reporter).Should().Be(0);

            var products = CompactProductStore.Read(compactPath).ToList();
            products.Should().HaveCount(4);
            products.Single(p => p.ParentAsin == "B000000001").ReviewCount.Should().Be(2);
            Assert_export_reports_every_step_through_to_completion(reporter);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>Every step the extraction runs appears, and the last report for it is its own completion.</summary>
    private static void Assert_export_reports_every_step_through_to_completion(RecordingProgressReporter reporter)
    {
        reporter.Steps().Should().ContainInOrder(
            "Scanning meta sources", "Selecting products", "Reading reviews", "Writing compact input");

        foreach (var step in reporter.Steps())
        {
            reporter.Last(step).Completed.Should().Be(reporter.Last(step).Total, $"step '{step}' must finish");
        }

        reporter.Summaries.Should().ContainSingle().Which.Should().Contain("wrote 4 products");
    }

    private static string Meta(string directory, string name, string row)
    {
        var path = Path.Combine(directory, name);
        File.WriteAllText(path, row + "\n");

        return path;
    }

    private static string Reviews(string directory, string name, params string[] rows)
    {
        var path = Path.Combine(directory, name);
        File.WriteAllText(path, string.Join("\n", rows));

        return path;
    }

    private static string Row(string asin, string title, string path) => JsonSerializer.Serialize(new
    {
        parent_asin = asin,
        title,
        description = new[] { "description" },
        categories = path.Split('|'),
        details = new Dictionary<string, object> { ["Brand"] = "X", ["Product Dimensions"] = "1x1", ["Color"] = "Black", ["Item Weight"] = "1 pound" },
        price = 120.0,
        average_rating = 4.5,
        rating_number = 10,
        store = "X",
        images = new[] { new { large = "https://example.com/i.jpg" } },
    });

    private static string Review(string asin) => JsonSerializer.Serialize(new
    {
        parent_asin = asin,
        rating = 5.0,
        title = "t",
        text = "good",
        helpful_vote = 1,
        timestamp = 1L,
    });
}