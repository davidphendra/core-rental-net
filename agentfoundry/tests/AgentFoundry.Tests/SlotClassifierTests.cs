using AgentFoundry.WorkspaceSuggestions.Contracts;
using AgentFoundry.WorkspaceSuggestions.Intent;
using AgentFoundry.WorkspaceSuggestions.Prompts;
using AwesomeAssertions;
using Xunit;

namespace AgentFoundry.Tests;

/// <summary>
/// The table's fallback, against a model that says what it is told.
/// </summary>
public sealed class SlotClassifierTests
{
    private static readonly IReadOnlyList<SlotRule> Rules =
    [
        new("Desk", "a desk", 1, true),
        new("Chair", "a chair", 1, true),
        new("Monitor", "a screen", 3, false),
        new("Plant", "something green", 5, false),
    ];

    private static SlotClassifier Classifier(ScriptedChatClient model)
        => new(model, PromptLibrary.Beside(AppContext.BaseDirectory));

    [Fact]
    public async Task The_parts_the_model_names_are_the_answer()
    {
        var names = await Classifier(new ScriptedChatClient("""["Desk","Chair","Plant"]"""))
            .ClassifyAsync("somewhere to think, and something green", Rules, []);

        names.Should().BeEquivalentTo("Desk", "Chair", "Plant");
    }

    /// <summary>A part that does not exist is dropped rather than returned.</summary>
    /// <remarks>
    /// The prompt says a name outside the list is a wrong answer rather than a new part, and this is the
    /// code keeping it that way - including for a name this workspace does not have, which is a narrower
    /// thing than the vocabulary.
    /// </remarks>
    [Fact]
    public async Task A_part_that_does_not_exist_is_dropped()
    {
        var model = new ScriptedChatClient("""["Desk","Hammock","CoffeeStation","Desk"]""");

        var names = await Classifier(model).ClassifyAsync("a hammock and a coffee machine", Rules, []);

        names.Should().BeEquivalentTo("Desk");
    }

    [Fact]
    public async Task An_empty_answer_is_an_empty_answer()
    {
        var names = await Classifier(new ScriptedChatClient("[]"))
            .ClassifyAsync("anything", Rules, []);

        names.Should().BeEmpty();
    }

    /// <summary>An answer that is not a list is refused rather than read as no parts.</summary>
    [Theory]
    [InlineData("""{"slots":["Desk"]}""")]
    [InlineData("none of them")]
    public async Task An_answer_that_is_not_a_list_is_refused(string answer)
    {
        var classify = async () => await Classifier(new ScriptedChatClient(answer))
            .ClassifyAsync("a desk", Rules, []);

        await classify.Should().ThrowAsync<PromptAnswerException>();
    }

    /// <summary>The prompt states the parts that exist, by both names the application has for them.</summary>
    [Fact]
    public async Task The_model_is_told_which_parts_exist()
    {
        var model = new ScriptedChatClient("[]");

        await Classifier(model).ClassifyAsync("a desk", Rules, []);

        model.Asked.Should().NotBeNull();
        model.Asked.Should().Contain("- Desk: \"a desk\"");
        model.Asked.Should().Contain("- Monitor: \"a screen\"");
        model.Asked.Should().NotContain("CoffeeStation", "a part this workspace does not have is not offered");
    }

    /// <summary>What a reviewer objected to last time reaches the model, because it is why it is asked again.</summary>
    [Fact]
    public async Task The_objections_from_last_time_reach_the_model()
    {
        var model = new ScriptedChatClient("[]");

        await Classifier(model).ClassifyAsync(
            "a desk",
            Rules,
            [new Finding("criteria_not_met", "Chair")]);

        model.Asked.Should().NotBeNull();
        model.Asked.Should().Contain("criteria_not_met on Chair");
    }
}
