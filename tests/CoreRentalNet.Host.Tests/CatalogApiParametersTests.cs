using AwesomeAssertions;
using CoreRentalNet.Host.Controllers;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The catalogue endpoint's query string, parsed from raw text. Unit level, because binding a filter
/// is a pure decision: which one was asked for, or why it cannot be answered.
/// </summary>
public sealed class CatalogApiParametersTests
{
    [Fact] // API-03
    public void No_filter_is_no_narrowing()
    {
        var binding = CatalogApiParameters.Bind(null, null, null);

        binding.IsValid.Should().BeTrue();
        binding.Query!.Category.Should().BeNull();
        binding.Query.SubCategory.Should().BeNull();
        binding.Query.Search.Should().BeNull();
    }

    [Theory] // API-02
    [InlineData("desk")]
    [InlineData("Desk")]
    [InlineData("DESK")]
    [InlineData("  desk  ")]
    public void A_category_is_matched_ignoring_case_and_surrounding_space(string value)
    {
        var binding = CatalogApiParameters.Bind(value, null, null);

        binding.IsValid.Should().BeTrue();
        binding.Query!.Category.Should().Be(CatalogCategory.Desk);
    }

    [Fact] // API-02
    public void A_subcategory_is_matched_ignoring_case()
    {
        var binding = CatalogApiParameters.Bind(null, "Monitor", null);

        binding.IsValid.Should().BeTrue();
        binding.Query!.SubCategory.Should().Be(CatalogSubCategory.Monitor);
    }

    [Fact] // API-01
    public void An_unknown_category_is_refused_and_names_the_allowed_values()
    {
        var binding = CatalogApiParameters.Bind("sofa", null, null);

        binding.IsValid.Should().BeFalse();
        binding.Query.Should().BeNull();
        binding.Error.Should().Contain("sofa").And.Contain("chair").And.Contain("desk").And.Contain("accessory");
    }

    [Fact] // API-01
    public void An_unknown_subcategory_is_refused_and_names_the_allowed_values()
    {
        var binding = CatalogApiParameters.Bind(null, "hammock", null);

        binding.IsValid.Should().BeFalse();
        binding.Error.Should().Contain("hammock").And.Contain("lamp").And.Contain("monitor").And.Contain("plant");
    }

    [Theory] // API-02
    [InlineData("1")]
    [InlineData("0")]
    public void A_number_is_not_a_category(string value)
        => CatalogApiParameters.Bind(value, null, null).IsValid.Should().BeFalse();

    [Fact] // API-03
    public void A_blank_search_is_no_narrowing()
    {
        var binding = CatalogApiParameters.Bind(null, null, "   ");

        binding.IsValid.Should().BeTrue();
        binding.Query!.Search.Should().BeNull();
    }

    [Fact] // API-05
    public void A_search_term_is_carried_through()
        => CatalogApiParameters.Bind(null, null, "teak").Query!.Search.Should().Be("teak");
}
