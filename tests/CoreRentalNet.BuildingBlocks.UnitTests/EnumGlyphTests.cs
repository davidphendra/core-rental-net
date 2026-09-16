using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Application;
using Xunit;

namespace CoreRentalNet.BuildingBlocks.UnitTests;

/// <summary>The reader that takes a glyph name off an enum member's attribute.</summary>
/// <remarks>
/// It is total on purpose: a member with no glyph reads as its own name rather than throwing, which
/// is what lets a vocabulary annotate only the members that are drawn.
/// </remarks>
public sealed class EnumGlyphTests
{
    [Fact]
    public void A_member_with_a_glyph_is_read_from_its_attribute()
        => SampleVocabulary.Annotated.Glyph().Should().Be("known");

    [Fact]
    public void A_member_without_a_glyph_falls_back_to_its_own_name()
        => SampleVocabulary.Unannotated.Glyph().Should().Be(nameof(SampleVocabulary.Unannotated));

    [Fact]
    public void The_label_reader_and_the_glyph_reader_do_not_share_a_slot()
    {
        // Two facts, two attributes: a member carries only one DescriptionAttribute, so storing the
        // glyph there instead would trade one of them away.
        SampleVocabulary.Annotated.Glyph().Should().Be("known");
        SampleVocabulary.Annotated.Label().Should().Be("The annotated kind");
    }
}
