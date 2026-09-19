using AwesomeAssertions;
using CoreRentalNet.Host.AiBuilder;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>AIWB-14 and AIWB-15: hygiene runs on a complete value, and it caps what it shows.</summary>
public sealed class OutputHygieneTests
{
    [Fact] // AIWB-14, the case the design exists for
    public void A_URL_split_across_chunks_is_still_stripped_because_hygiene_sees_it_whole()
    {
        const string json =
            """{"options":[{"rationale":"See https://example.invalid/very/long/path for details."}]}""";

        var reader = new NarrativeFieldReader();
        var shown = new List<string>();

        // One character at a time, so the URL is split at every possible point, in every field.
        foreach (var character in json)
        {
            foreach (var field in reader.Feed(character.ToString()))
            {
                shown.Add(OutputHygiene.Apply(field.Kind, field.Text));
            }
        }

        shown.Should().ContainSingle();
        shown[0].Should().NotContain("http");
        shown[0].Should().NotContain("example.invalid");
        shown[0].Should().Be("See for details.");
    }

    [Fact] // AIWB-15
    public void A_rationale_longer_than_the_cap_is_truncated_to_the_cap()
    {
        var long_ = string.Join(" ", Enumerable.Repeat("spacious", 60));

        var capped = OutputHygiene.Apply(NarrativeFieldKind.Rationale, long_);

        capped.Length.Should().BeLessThanOrEqualTo(OutputHygiene.RationaleCap);
        capped.Should().EndWith("…");
        capped.Should().StartWith("spacious spacious");
    }

    [Fact] // AIWB-15
    public void A_line_s_why_is_capped_shorter_than_a_rationale()
    {
        var long_ = string.Join(" ", Enumerable.Repeat("steady", 40));

        OutputHygiene.Apply(NarrativeFieldKind.Why, long_).Length
            .Should().BeLessThanOrEqualTo(OutputHygiene.WhyCap);
        OutputHygiene.WhyCap.Should().BeLessThan(OutputHygiene.RationaleCap);
    }

    [Fact]
    public void Text_within_the_cap_is_returned_untouched()
    {
        const string text = "A calm, focused setup for a small room.";

        OutputHygiene.Apply(NarrativeFieldKind.Rationale, text).Should().Be(text);
        OutputHygiene.Apply(NarrativeFieldKind.Why, text).Should().Be(text);
    }

    [Theory]
    [InlineData("Costs Rp 1.200.000 a month.", "Costs a month.")]
    [InlineData("About IDR 3500000 for the set.", "About for the set.")]
    [InlineData("Under $120 and worth it.", "Under and worth it.")]
    [InlineData("Roughly 3500000 IDR.", "Roughly.")]
    public void A_currency_amount_is_stripped(string given, string expected)
    {
        OutputHygiene.Apply(NarrativeFieldKind.Rationale, given).Should().Be(expected);
    }

    [Fact]
    public void Markup_is_stripped_and_a_markdown_link_keeps_only_its_label()
    {
        OutputHygiene.Apply(NarrativeFieldKind.Rationale, "A <b>tidy</b> corner for <i>two</i>.")
            .Should().Be("A tidy corner for two.");

        OutputHygiene.Apply(NarrativeFieldKind.Rationale, "See the [catalogue](/products) page.")
            .Should().Be("See the catalogue page.");
    }

    [Fact]
    public void Free_text_only_is_touched_and_whitespace_is_collapsed()
    {
        // Nothing here invents a value: the text it is given comes back shorter, never different.
        OutputHygiene.Apply(NarrativeFieldKind.Why, "  a    stable   surface  ").Should().Be("a stable surface");
        OutputHygiene.Apply(NarrativeFieldKind.Why, string.Empty).Should().BeEmpty();
    }
}
