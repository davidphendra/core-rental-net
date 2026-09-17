using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Infrastructure;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// The running application must not fetch a product image from a third party: the E2E suite is
/// required to be hermetic, and an external host is also a dependency nobody asked for.
/// </summary>
/// <remarks>
/// The vendoring rule is exercised with a synthetic remote path and a temporary web root rather than
/// with rows of the real catalogue. The rule belongs to the resolver, so a test of it should not
/// depend on which products happen to be vendored - and the shipped catalogue carries no remote
/// image at all, which the first test asserts.
/// </remarks>
public sealed class VendoredImageTests
{
    private static string WebRoot => RepoRoot.Combine("src", "Host", "CoreRentalNet.Host", "wwwroot");

    private static string ProductsJson => RepoRoot.Combine("src", "shared", "data", "products.json");

    [Fact] // UI-06
    public void The_vendored_images_resolve_to_real_files()
    {
        var catalog = new ProductCatalog(ProductsJson, WebRoot);

        var vendored = catalog.All
            .Where(product => product.ImagePath.StartsWith("/images/vendored/", StringComparison.Ordinal))
            .ToArray();

        // A few products carry a remote image path, and the resolver replaces it with the local copy
        // named by SKU. The UI renders a real photograph for these, which is the path the layout
        // tests exercise, so the catalogue has to keep holding some.
        vendored.Should().NotBeEmpty();

        foreach (var product in vendored)
        {
            product.ImageAvailable.Should().BeTrue($"{product.Name} has a vendored file");
            File.Exists(Path.Combine(WebRoot, product.ImagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)))
                .Should().BeTrue($"{product.ImagePath} must exist on disk");
        }
    }

    [Fact] // UI-06
    public void The_real_catalog_carries_no_unvendored_remote_image_path()
    {
        var catalog = new ProductCatalog(ProductsJson, WebRoot);

        catalog.All.Should().NotContain(
            product => product.ImagePath.StartsWith("http", StringComparison.OrdinalIgnoreCase),
            "every product's image is served from this application");
    }

    [Fact] // UI-06
    public void A_remote_image_with_a_vendored_copy_resolves_to_the_local_file()
    {
        using var webRoot = new TemporaryDirectory();
        var vendored = Path.Combine(webRoot.Path, "images", "vendored");
        Directory.CreateDirectory(vendored);
        File.WriteAllBytes(Path.Combine(vendored, "AAA0001.png"), [1, 2, 3]);

        using var file = new TemporaryFile(
            """
            [{ "skuNo": "AAA0001", "name": "Remote", "category": "chair", "pricePerMonth": 100, "description": "d", "image": "https://example.invalid/remote.png", "metadata": { "tags": ["x"], "attributes": { "k": "v" }, "bestFor": [], "notFor": [] } }]
            """);

        var product = new ProductCatalog(file.Path, webRoot.Path).Find("AAA0001")!;

        product.ImagePath.Should().Be("/images/vendored/AAA0001.png", "a remote image is replaced by its local copy");
        product.ImageAvailable.Should().BeTrue();
    }

    [Fact] // CAT-12, UI-05
    public void Products_without_an_image_file_are_flagged_so_the_ui_can_draw_a_placeholder()
    {
        var catalog = new ProductCatalog(ProductsJson, WebRoot);

        var missing = catalog.All.Where(product => !product.ImageAvailable).ToArray();

        missing.Should().NotBeEmpty("most catalog images are placeholders that were never shipped");
        missing.Should().OnlyContain(product => product.ImagePath.StartsWith("/placeholders/", StringComparison.Ordinal));
    }
}
