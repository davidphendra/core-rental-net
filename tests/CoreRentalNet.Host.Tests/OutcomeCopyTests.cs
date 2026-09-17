using CoreRentalNet.Host.Presentation;
using CoreRentalNet.Modules.Workspace.Application.Contracts.Suggestion;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// What the AI section says, held to the two rules it must never break.
/// </summary>
/// <remarks>
/// <para>
/// The agent sends codes and the application owns the sentences, so these tests are really about the
/// channel: there must be no path from a code - or from anything else the agent produced - to a sentence
/// a customer reads, except the ones written here.
/// </para>
/// <para>
/// The two rules are that no raw code is ever rendered, and that the customer's own words are the only
/// text that came from outside.
/// </para>
/// </remarks>
public sealed class OutcomeCopyTests
{
    [Fact]
    public void A_known_refusal_is_said_in_the_applications_words()
    {
        var words = OutcomeCopy.Refusal("not_workspace_request");

        Assert.False(string.IsNullOrWhiteSpace(words));
        Assert.DoesNotContain("not_workspace_request", words, StringComparison.Ordinal);
    }

    /// <summary>
    /// A code the application has not been taught renders a sentence, never the code.
    /// </summary>
    /// <remarks>
    /// This is the test that guards the channel. A fallback that printed the code would look like
    /// robustness and would be the agent's vocabulary appearing on the page, which is exactly what the
    /// contract's ban on prose is meant to make impossible.
    /// </remarks>
    [Fact]
    public void An_unknown_code_renders_a_sentence_and_never_the_code()
    {
        foreach (var code in new[] { "some_future_code", "CRITERIA_NOT_MET", "", "  " })
        {
            var words = OutcomeCopy.Refusal(code);

            Assert.False(string.IsNullOrWhiteSpace(words));

            if (!string.IsNullOrWhiteSpace(code))
            {
                Assert.DoesNotContain(code.Trim(), words, StringComparison.Ordinal);
            }
        }

        Assert.False(string.IsNullOrWhiteSpace(OutcomeCopy.Refusal(code: null)));
    }

    [Theory]
    [InlineData("low", "Essential")]
    [InlineData("middle", "Balanced")]
    [InlineData("high", "Premium")]
    public void A_tier_is_named_rather_than_echoed(string tier, string words)
        => Assert.Equal(words, OutcomeCopy.Tier(tier));

    [Theory]
    [InlineData("cheap")]
    [InlineData("")]
    [InlineData(null)]
    public void An_unknown_tier_is_a_word_and_not_the_token(string? tier)
    {
        var words = OutcomeCopy.Tier(tier);

        Assert.False(string.IsNullOrWhiteSpace(words));

        if (!string.IsNullOrWhiteSpace(tier))
        {
            Assert.DoesNotContain(tier, words, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// A criterion arrives as a token and leaves as words: a token on a page is an agent's vocabulary.
    /// </summary>
    [Theory]
    [InlineData("slot:chair", "chair")]
    [InlineData("quantity:monitor:2", "monitor 2")]
    [InlineData("tag:standing", "standing")]
    [InlineData("attribute:desk:type:sit-stand", "desk type sit stand")]
    public void A_criterion_token_is_read_out_as_words(string token, string expected)
        => Assert.Equal(expected, OutcomeCopy.Criterion(token));

    [Theory]
    [InlineData("nonsense")]
    [InlineData("slot:")]
    [InlineData("")]
    [InlineData(null)]
    public void A_criterion_that_is_not_a_token_says_something_rather_than_nothing(string? token)
    {
        var words = OutcomeCopy.Criterion(token);

        Assert.False(string.IsNullOrWhiteSpace(words));

        if (!string.IsNullOrWhiteSpace(token))
        {
            Assert.DoesNotContain(token, words, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>An objection names the thing it is about, which is why the slot crosses the boundary.</summary>
    [Fact]
    public void An_objection_is_placed_on_the_slot_it_is_about()
    {
        var chair = OutcomeCopy.Finding(new SuggestionFinding("criteria_not_met", "Chair"));

        Assert.Contains("chair", chair, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("criteria_not_met", chair, StringComparison.Ordinal);

        // The contract's compound names are opened up rather than left as one word.
        var station = OutcomeCopy.Finding(new SuggestionFinding("tier_composition", "CoffeeStation"));

        Assert.Contains("coffee station", station, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void An_objection_we_do_not_know_still_names_the_slot()
    {
        var words = OutcomeCopy.Finding(new SuggestionFinding("a_future_kind", "Desk"));

        Assert.Contains("desk", words, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("a_future_kind", words, StringComparison.Ordinal);
    }

    /// <summary>
    /// The two outcomes the application says on its own, and which must not read as each other.
    /// </summary>
    [Fact]
    public void The_caveat_and_the_error_say_different_things()
    {
        Assert.NotEqual(OutcomeCopy.Caveat, OutcomeCopy.Unreachable);

        // A caveat qualifies a result; the error is about a run that produced none.
        Assert.DoesNotContain("reach", OutcomeCopy.Caveat, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("reach", OutcomeCopy.Unreachable, StringComparison.OrdinalIgnoreCase);
    }
}
