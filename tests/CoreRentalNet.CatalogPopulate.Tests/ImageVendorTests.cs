using Xunit;
using AwesomeAssertions;
using CoreRentalNet.CatalogPopulate.Images;

namespace CoreRentalNet.CatalogPopulate.Tests;

public class ImageVendorTests
{
    [Fact]
    public async Task Vendoring_reports_each_image_and_ends_on_its_total()
    {
        using var directory = new TemporaryDirectory();
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "CHA000000001.jpg"), "bytes");

        var images = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["CHA000000001"] = "https://example.com/a.jpg",
            ["CHA000000002"] = null,
        };

        var reporter = new RecordingProgressReporter();
        var vendored = await new ImageVendor(new HttpClient(), directory.Path)
            .VendorAsync(images, reporter, CancellationToken.None);

        vendored.Kept.Should().Be(1);
        vendored.Downloaded.Should().Be(0);
        reporter.Last("Vendoring images").Completed.Should().Be(2);
        reporter.Last("Vendoring images").Total.Should().Be(2);
    }
}
