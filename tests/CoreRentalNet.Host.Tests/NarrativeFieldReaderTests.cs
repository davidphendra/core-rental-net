using AwesomeAssertions;
using CoreRentalNet.Host.AiBuilder;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// AIWB-13 at unit level: the reader's half of "no raw JSON is ever rendered", plus the chunk-boundary
/// cases the whole design turns on.
/// </summary>
/// <remarks>
/// The two constants are the real wire, captured by running the host against a scripted model client: the
/// rephraser's specification first, the suggestor's result last, as two top-level objects joined by a
/// newline. Feeding them one character at a time puts a chunk boundary inside every escape, every URL and
/// every word.
/// </remarks>
public sealed class NarrativeFieldReaderTests
{
    private const string Spec =
        """
        { "status": "spec", "reason": null, "ceilingMonthly": 1500000,
          "slots": [ { "slot": "Desk", "quantity": 1, "purpose": "a wide, stable surface" } ],
          "constraints": [ "a small room" ] }
        """;

    private const string Result =
        """
        { "status": "suggested", "reason": null,
          "options": [ { "lines": [ { "slot": "Desk", "sku": "DSKB08XN4JDR", "quantity": 1, "why": "a stable surface" } ],
                         "rationale": "A calm, focused setup for a small room." } ],
          "usage": { "modelCalls": 2, "inputTokens": 172000, "outputTokens": 400,
                     "model": "gpt-4.1-mini", "promptVersion": "suggestor.v1" } }
        """;

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(17)]
    [InlineData(500)]
    public void The_result_s_narrative_fields_are_emitted_in_order_whatever_the_chunking(int chunk)
    {
        var fields = FeedAll(Spec + "\n" + Result, chunk);

        fields.Should().HaveCount(2, "the specification has no narrative field and the result has two");
        fields[0].Kind.Should().Be(NarrativeFieldKind.Why);
        fields[0].Text.Should().Be("a stable surface");
        fields[1].Kind.Should().Be(NarrativeFieldKind.Rationale);
        fields[1].Text.Should().Be("A calm, focused setup for a small room.");
    }

    [Fact] // AIWB-13
    public void No_raw_JSON_is_emitted_at_any_point_of_the_stream()
    {
        var emitted = new List<string>();
        var reader = new NarrativeFieldReader();

        foreach (var character in Spec + "\n" + Result)
        {
            emitted.AddRange(reader.Feed(character.ToString()).Select(field => field.Text));
        }

        emitted.Should().OnlyContain(text =>
            !text.Contains('{') && !text.Contains('}') && !text.Contains('"')
            && !text.Contains(':') && !text.Contains('[') && !text.Contains(']'));

        emitted.Should().NotContain(text => text.Contains("sku", StringComparison.OrdinalIgnoreCase));
        emitted.Should().NotContain(text => text.Contains("status", StringComparison.OrdinalIgnoreCase));
        emitted.Should().Equal("a stable surface", "A calm, focused setup for a small room.");
    }

    [Fact]
    public void A_value_that_has_not_closed_is_not_emitted()
    {
        var reader = new NarrativeFieldReader();
        var half = Result.IndexOf("A calm, focused", StringComparison.Ordinal) + 8;

        // The line's why comes first in the document and has already closed, so it is already out; the
        // rationale, which is the one being cut through, must not be.
        reader.Feed(Result[..half]).Should().NotContain(field => field.Kind == NarrativeFieldKind.Rationale);

        reader.Feed(Result[half..]).Should().ContainSingle()
            .Which.Text.Should().Be("A calm, focused setup for a small room.");
    }

    [Fact]
    public void An_escaped_quote_and_a_newline_survive_the_split()
    {
        var reader = new NarrativeFieldReader();
        var fields = new List<NarrativeField>();

        // The split is inside the escape sequence itself, which is the worst place for one.
        foreach (var character in """{"options":[{"rationale":"line\nbreak and a \"quote\""}]}""")
        {
            fields.AddRange(reader.Feed(character.ToString()));
        }

        fields.Should().ContainSingle();
        fields[0].Text.Should().Be("line\nbreak and a \"quote\"");
    }

    [Fact]
    public void Text_that_is_not_JSON_at_all_does_not_throw_and_does_not_lose_earlier_fields()
    {
        var reader = new NarrativeFieldReader();

        reader.Feed(Result).Should().HaveCount(2);

        // A model that starts a preamble, or a transport that appends junk, must not take the run down.
        reader.Feed("Certainly! Here is the reasoning").Should().BeEmpty();
    }

    [Fact]
    public void The_specification_contributes_no_narrative_field()
    {
        var fields = FeedAll(Spec, 5);

        fields.Should().BeEmpty("'purpose' is the specification's own word and is not streamed to anyone");
    }

    private static IReadOnlyList<NarrativeField> FeedAll(string text, int chunk)
    {
        var reader = new NarrativeFieldReader();
        var fields = new List<NarrativeField>();

        for (var at = 0; at < text.Length; at += chunk)
        {
            fields.AddRange(reader.Feed(text.Substring(at, Math.Min(chunk, text.Length - at))));
        }

        return fields;
    }
}
