using CoreRentalNet.Host.Presentation;
using AwesomeAssertions;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The words the page shows while a run is happening, and what it shows for an id it does not know.
/// </summary>
/// <remarks>
/// The list is written out here rather than read from the agent, because the agent is a separate
/// solution and reading its vocabulary would be the coupling this split exists to avoid. That makes this
/// the place drift is caught: a stage added there and not here fails a test rather than quietly
/// disappearing from a customer's screen.
/// </remarks>
public sealed class SuggestionStageCopyTests
{
    [Theory] // AIB-11
    [InlineData("verifying")]
    [InlineData("rephrasing")]
    [InlineData("selecting")]
    [InlineData("reviewing")]
    public void Every_stage_the_agent_publishes_has_words_of_its_own(string stage)
    {
        var words = SuggestionStageCopy.For(stage);

        words.Should().NotBeNullOrWhiteSpace();
        words.Should().NotContain(stage, "the page shows the application's words, never the agent's id");
    }

    [Theory] // AIB-11
    [InlineData("stage-from-a-later-release")]
    [InlineData("")]
    [InlineData(null)]
    public void A_stage_this_application_does_not_know_renders_nothing(string? stage)
        => SuggestionStageCopy.For(stage).Should().BeNull("an unknown id is not something to show a customer");
}
