using CoreRentalNet.Host.Presentation;
using CoreRentalNet.Modules.Workspace.Application.Contracts.Suggestion;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// How many candidates a customer is shown, and which one.
/// </summary>
/// <remarks>
/// The rule is that the power permission decides three and anything else entitled sees one, at the
/// middle: the neutral choice, neither the cheapest available nor the most expensive. Which one is
/// shown is as much the rule as how many, so both are asserted.
/// </remarks>
public sealed class CandidateCountTests
{
    [Fact]
    public void The_power_permission_shows_all_three()
    {
        var visible = CandidateChoices.Visible([Tier("low"), Tier("middle"), Tier("high")], entitledToThree: true);

        Assert.Equal(["low", "middle", "high"], visible.Select(option => option.Tier));
    }

    /// <summary>The one candidate an ordinary entitled account sees is the middle one.</summary>
    [Fact]
    public void Without_it_one_candidate_is_shown_and_it_is_the_middle()
    {
        var visible = CandidateChoices.Visible([Tier("low"), Tier("middle"), Tier("high")], entitledToThree: false);

        Assert.Equal("middle", Assert.Single(visible).Tier);
    }

    /// <summary>
    /// A run that composed fewer than three is not padded, and the middle is still what is meant.
    /// </summary>
    /// <remarks>
    /// With two candidates the lower of the two middles is shown, which is the cheaper - and that is
    /// deliberate: "the middle" means the neutral pick, and with two there is none, so the rule falls to
    /// the one that is not the dearest.
    /// </remarks>
    [Fact]
    public void Fewer_than_three_is_not_padded()
    {
        Assert.Equal("low", Assert.Single(CandidateChoices.Visible([Tier("low"), Tier("high")], entitledToThree: false)).Tier);
        Assert.Equal("low", Assert.Single(CandidateChoices.Visible([Tier("low")], entitledToThree: false)).Tier);
        Assert.Empty(CandidateChoices.Visible([], entitledToThree: false));
        Assert.Equal(2, CandidateChoices.Visible([Tier("low"), Tier("high")], entitledToThree: true).Count);
    }

    /// <summary>
    /// The rule is applied to what is rendered, not to what is in the document and styled away.
    /// </summary>
    /// <remarks>
    /// A hidden candidate is still a control: it can be reached by looking, by a screen reader, and by
    /// anything that reads the markup. This asserts the choice is made before rendering by asserting it is
    /// made at all - the list that leaves this method is the list the page has.
    /// </remarks>
    [Fact]
    public void The_unshown_candidates_are_absent_rather_than_hidden()
    {
        var options = new[] { Tier("low"), Tier("middle"), Tier("high") };

        Assert.DoesNotContain("low", CandidateChoices.Visible(options, false).Select(option => option.Tier));
        Assert.DoesNotContain("high", CandidateChoices.Visible(options, false).Select(option => option.Tier));
    }

    private static SuggestedOption Tier(string tier)
        => new(tier, [], [], [], []);
}
