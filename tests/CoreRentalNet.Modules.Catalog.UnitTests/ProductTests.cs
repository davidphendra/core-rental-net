using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Domain;
using Xunit;

namespace CoreRentalNet.Modules.Catalog.UnitTests;

public sealed class ProductTests
{
    [Fact]
    public void An_accessory_requires_a_subcategory()
    {
        var action = () => new Product(
            Sku.Of("MON0001"), "Monitor", ProductCategory.Accessory, null,
            Money.Idr(300000m), "A monitor.", "/images/monitor.svg", null, true);

        action.Should().Throw<DomainRuleViolationException>().WithMessage("*subcategory*");
    }

    [Fact]
    public void A_chair_cannot_carry_a_subcategory()
    {
        var action = () => new Product(
            Sku.Of("CHA0001"), "Chair", ProductCategory.Chair, ProductSubCategory.Monitor,
            Money.Idr(400000m), "A chair.", "/images/chair.svg", null, true);

        action.Should().Throw<DomainRuleViolationException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_product_requires_a_name(string name)
    {
        var action = () => new Product(
            Sku.Of("CHA0001"), name, ProductCategory.Chair, null,
            Money.Idr(400000m), "A chair.", "/images/chair.svg", null, true);

        action.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void A_product_requires_a_price()
    {
        var action = () => new Product(
            Sku.Of("CHA0001"), "Chair", ProductCategory.Chair, null,
            null!, "A chair.", "/images/chair.svg", null, true);

        action.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Only_the_popular_badge_makes_a_product_featured()
    {
        SampleCatalog.Chair("CHA0001", 400000m).IsFeatured.Should().BeFalse();
        SampleCatalog.Chair("CHA0002", 400000m, ProductBadge.Popular).IsFeatured.Should().BeTrue();
    }
}
