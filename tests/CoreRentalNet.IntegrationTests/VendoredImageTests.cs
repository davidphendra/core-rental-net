using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Infrastructure.Loading;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// The running application must not fetch a product image from a third party: the E2E suite is
/// required to be hermetic, and an external host is also a dependency nobody asked for.
/// </summary>
public sealed class VendoredImageTests
{
    private static string WebRoot => RepoRoot.Combine("src", "Host", "CoreRentalNet.Host", "wwwroot");

    private static string ProductsJson => RepoRoot.Combine("src", "shared", "data", "products.json");

    [Fact] // UI-06
    public void Loading_the_real_catalog_with_the_web_root_leaves_no_remote_image_path()
    {
        var catalog = CatalogLoader.LoadFromFile(ProductsJson, WebRoot);

        catalog.All.Should().NotContain(
            product => product.ImagePath.StartsWith("http", StringComparison.OrdinalIgnoreCase),
            "every remote image must have been vendored into wwwroot");
    }

    [Fact] // UI-06
    public void The_vendored_images_resolve_to_real_files()
    {
        var catalog = CatalogLoader.LoadFromFile(ProductsJson, WebRoot);

        var vendored = catalog.All
            .Where(product => product.ImagePath.StartsWith("/images/vendored/", StringComparison.Ordinal))
            .ToArray();

        vendored.Should().NotBeEmpty();

        foreach (var product in vendored)
        {
            product.ImageAvailable.Should().BeTrue($"{product.Name} has a vendored file");
            File.Exists(Path.Combine(WebRoot, product.ImagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)))
                .Should().BeTrue($"{product.ImagePath} must exist on disk");
        }
    }

    [Fact] // CAT-12, UI-05
    public void Products_without_an_image_file_are_flagged_so_the_ui_can_draw_a_placeholder()
    {
        var catalog = CatalogLoader.LoadFromFile(ProductsJson, WebRoot);

        var missing = catalog.All.Where(product => !product.ImageAvailable).ToArray();

        missing.Should().NotBeEmpty("most catalog images are placeholders that were never shipped");
        missing.Should().OnlyContain(product => product.ImagePath.StartsWith("/placeholders/", StringComparison.Ordinal));
    }
}
