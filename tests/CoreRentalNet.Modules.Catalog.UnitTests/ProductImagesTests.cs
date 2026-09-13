using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Domain;
using CoreRentalNet.Modules.Catalog.Infrastructure.Loading;
using Xunit;

namespace CoreRentalNet.Modules.Catalog.UnitTests;

/// <summary>The two image rules, without a catalog file in the way.</summary>
public sealed class ProductImagesTests
{
    private static readonly Sku Known = Sku.Of("DSK0001");

    [Fact] // CAT-53
    public void A_remote_image_is_assumed_to_be_there()
    {
        var image = new ProductImages(webRootPath: null).Resolve(Known, "https://example.test/desk.png");

        image.Path.Should().Be("https://example.test/desk.png");
        image.Available.Should().BeTrue("the application cannot check a host it does not serve");
    }

    [Fact] // CAT-53
    public void A_local_image_with_no_web_root_is_unavailable()
        => new ProductImages(webRootPath: null)
            .Resolve(Known, "/images/desk.svg")
            .Available.Should().BeFalse();

    [Fact] // CAT-53
    public void A_local_image_is_available_only_when_the_file_is_there()
    {
        using var webRoot = new TemporaryWebRoot();
        File.WriteAllText(Path.Combine(webRoot.Root, "desk.svg"), "<svg />");

        var images = new ProductImages(webRoot.Root);

        images.Resolve(Known, "/desk.svg").Available.Should().BeTrue();
        images.Resolve(Known, "/missing.svg").Available.Should().BeFalse();
    }

    [Fact] // CAT-53
    public void A_vendored_copy_replaces_a_remote_image()
    {
        using var webRoot = new TemporaryWebRoot();

        var vendored = Path.Combine(webRoot.Root, "images", "vendored");
        Directory.CreateDirectory(vendored);
        File.WriteAllText(Path.Combine(vendored, "DSK0001.svg"), "<svg />");

        var image = new ProductImages(webRoot.Root).Resolve(Known, "https://example.test/desk.png");

        image.Path.Should().Be("/images/vendored/DSK0001.svg");
        image.Available.Should().BeTrue();
    }

    private sealed class TemporaryWebRoot : IDisposable
    {
        public TemporaryWebRoot() => Root = Directory.CreateTempSubdirectory("corerental-images-").FullName;

        public string Root { get; }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
