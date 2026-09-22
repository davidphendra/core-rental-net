using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using Xunit;

namespace CoreRentalNet.Modules.Catalog.UnitTests;

/// <summary>
/// The words the catalogService publishes for its own vocabulary.
/// </summary>
/// <remarks>
/// The labels are what the tabs, the group headings and the search all name things by, so a missing
/// one is not a display detail: it is a product the search can no longer find by its kind.
/// </remarks>
public sealed class CatalogLabelTests
{
    [Fact] // CAT-12
    public void Every_category_and_subcategory_has_a_label()
    {
        foreach (var category in Enum.GetValues<CatalogCategory>())
        {
            category.Label().Should().NotBeNullOrWhiteSpace();
        }

        foreach (var subCategory in Enum.GetValues<CatalogSubCategory>())
        {
            subCategory.Label().Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact] // CAT-12
    public void A_label_is_not_the_identifier_it_describes()
    {
        // "Coffee Machines" and "Bean bags" are the reason the label is an attribute rather than the
        // member's own name. If they ever match, the attribute stopped earning its keep.
        CatalogSubCategory.Coffee.Label().Should().NotBe(nameof(CatalogSubCategory.Coffee));
        CatalogSubCategory.Beanbag.Label().Should().NotBe(nameof(CatalogSubCategory.Beanbag));
    }

    [Fact] // CAT-12
    public void An_enum_without_a_label_falls_back_to_its_member_name()
    {
        // The helper is total: a member with no attribute is still readable, which is what keeps a
        // vocabulary from having to annotate every member and keeps an unannotated one from throwing.
        DayOfWeek.Monday.Label().Should().Be(nameof(DayOfWeek.Monday));
    }
}
