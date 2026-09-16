using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Infrastructure.Contracts;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Infrastructure;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// Exercises the real catalog file. This is the test that would fail if someone edited
/// products.json into an unusable state.
/// </summary>
public sealed class CatalogFileTests
{
    private static string ProductsJson => RepoRoot.Combine("src", "shared", "data", "products.json");

    [Fact] // CAT-01
    public void The_real_catalog_file_loads_with_the_expected_shape()
    {
        var catalog = new ProductCatalog(ProductsJson, null);

        catalog.All.Should().HaveCount(62);

        catalog.ByCategory(CatalogCategory.Chair).Should().HaveCount(10);
        catalog.ByCategory(CatalogCategory.Desk).Should().HaveCount(10);
        catalog.ByCategory(CatalogCategory.Accessory).Should().HaveCount(42);

        catalog.BySubCategory(CatalogSubCategory.Beanbag).Should().HaveCount(10);
        catalog.BySubCategory(CatalogSubCategory.Coffee).Should().HaveCount(10);
        catalog.BySubCategory(CatalogSubCategory.Lamp).Should().HaveCount(6);
        catalog.BySubCategory(CatalogSubCategory.Monitor).Should().HaveCount(8);
        catalog.BySubCategory(CatalogSubCategory.Plant).Should().HaveCount(8);
    }

    [Fact] // CAT-01
    public void Every_real_product_is_usable()
    {
        var catalog = new ProductCatalog(ProductsJson, null);

        catalog.All.Should().OnlyContain(product => !string.IsNullOrWhiteSpace(product.Name));
        catalog.All.Should().OnlyContain(product => !string.IsNullOrWhiteSpace(product.Description));
        catalog.All.Should().OnlyContain(product => !string.IsNullOrWhiteSpace(product.ImagePath));
        catalog.All.Should().OnlyContain(product => product.MonthlyPrice.Currency == Currencies.Idr);
        catalog.All.Should().OnlyContain(product => product.MonthlyPrice.Amount > 0m);
        catalog.All.Should().OnlyContain(
            product => product.Category == CatalogCategory.Accessory || product.SubCategory == null);
        catalog.All.Should().OnlyContain(
            product => product.Category != CatalogCategory.Accessory || product.SubCategory != null);
    }

    [Fact] // CAT-08
    public void The_real_catalog_has_exactly_two_featured_products()
    {
        var featured = new ProductCatalog(ProductsJson, null).Featured();

        featured.Should().HaveCount(2);
        featured.Select(product => product.Name).Should().Contain("Canggu Task").And.Contain("Monstera Plant");
    }

    [Fact] // CAT-09
    public void The_real_catalog_contains_no_partner_product()
    {
        var catalog = new ProductCatalog(ProductsJson, null);

        catalog.All.Should().NotContain(product => product.Name.Contains("Motorcycle", StringComparison.OrdinalIgnoreCase));
    }

    [Fact] // CAT-02
    public void A_malformed_catalog_file_fails_loudly_with_the_path()
    {
        using var file = new TemporaryFile("{ \"this is\" not json ]");

        var action = () => _ = new ProductCatalog(file.Path, null);

        action.Should().Throw<ProductLoadException>()
            .WithMessage($"*not valid JSON*{file.Path}*");
    }

    [Fact] // CAT-03
    public void A_missing_catalog_file_fails_loudly_with_the_path()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"core-rental-missing-{Guid.NewGuid():N}.json");

        var action = () => _ = new ProductCatalog(missing, null);

        action.Should().Throw<ProductLoadException>()
            .WithMessage($"*not found*{missing}*");
    }

    [Fact] // CAT-02
    public void An_empty_catalog_file_fails_loudly()
    {
        using var file = new TemporaryFile("[]");

        var action = () => _ = new ProductCatalog(file.Path, null);

        action.Should().Throw<ProductLoadException>().WithMessage("*no products*");
    }

    [Fact] // CAT-02
    public void An_unknown_category_fails_loudly_and_names_the_sku()
    {
        using var file = new TemporaryFile(
            """[{ "skuNo": "AAA0001", "name": "Thing", "category": "spaceship", "pricePerMonth": 100, "description": "d", "image": "/i.svg" }]""");

        var action = () => _ = new ProductCatalog(file.Path, null);

        action.Should().Throw<ProductLoadException>().WithMessage("*AAA0001*spaceship*");
    }

    [Fact] // CAT-14
    public void Duplicate_skus_are_rejected()
    {
        using var file = new TemporaryFile(
            """
            [{ "skuNo": "AAA0001", "name": "One", "category": "chair", "pricePerMonth": 100, "description": "d", "image": "/i.svg" },
             { "skuNo": "aaa0001", "name": "Two", "category": "chair", "pricePerMonth": 200, "description": "d", "image": "/i.svg" }]
            """);

        var action = () => _ = new ProductCatalog(file.Path, null);

        action.Should().Throw<ProductLoadException>().WithMessage("*duplicate*AAA0001*");
    }

    [Fact] // CAT-12
    public void An_image_that_does_not_exist_on_disk_is_marked_unavailable()
    {
        using var webRoot = new TemporaryDirectory();
        File.WriteAllText(Path.Combine(webRoot.Path, "present.svg"), "<svg />");

        using var file = new TemporaryFile(
            """
            [{ "skuNo": "AAA0001", "name": "Present", "category": "chair", "pricePerMonth": 100, "description": "d", "image": "/present.svg" },
             { "skuNo": "AAA0002", "name": "Absent", "category": "chair", "pricePerMonth": 100, "description": "d", "image": "/placeholders/absent.svg" }]
            """);

        var catalog = new ProductCatalog(file.Path, webRoot.Path);

        catalog.Find("AAA0001")!.ImageAvailable.Should().BeTrue();
        catalog.Find("AAA0002")!.ImageAvailable.Should().BeFalse();
    }

    [Fact] // CAT-12
    public void Without_a_web_root_local_images_are_assumed_unavailable_but_remote_ones_are_not()
    {
        using var file = new TemporaryFile(
            """
            [{ "skuNo": "AAA0001", "name": "Local", "category": "chair", "pricePerMonth": 100, "description": "d", "image": "/placeholders/local.svg" },
             { "skuNo": "AAA0002", "name": "Remote", "category": "chair", "pricePerMonth": 100, "description": "d", "image": "https://example.invalid/remote.png" }]
            """);

        var catalog = new ProductCatalog(file.Path, null);

        catalog.Find("AAA0001")!.ImageAvailable.Should().BeFalse();
        catalog.Find("AAA0002")!.ImageAvailable.Should().BeTrue();
    }

    [Fact] // CAT-02
    public void A_product_missing_its_image_path_fails_loudly()
    {
        using var file = new TemporaryFile(
            """[{ "skuNo": "AAA0001", "name": "No image", "category": "chair", "pricePerMonth": 100, "description": "d" }]""");

        var action = () => _ = new ProductCatalog(file.Path, null);

        action.Should().Throw<ProductLoadException>().WithMessage("*AAA0001*image*");
    }
}
