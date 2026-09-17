using AgentFoundry.WorkspaceSuggestions.Contracts;
using AgentFoundry.WorkspaceSuggestions.Intent;
using AgentFoundry.WorkspaceSuggestions.Review;
using AgentFoundry.WorkspaceSuggestions.Selection;
using AgentFoundry.WorkspaceSuggestions.Specifications;
using AgentFoundry.WorkspaceSuggestions.Workflows;
using AgentFoundry.WorkspaceSuggestions.Vocabularies;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AgentFoundry.Tests;

/// <summary>
/// The gate: what can be checked is checked in code, and only what cannot is judged.
/// </summary>
/// <remarks>
/// The reviewer holds no catalogue, so what it checks is the composition against the receipt the
/// suggestor sent. That is exactly the set of failures that are wrong without looking wrong, which is
/// why they are checked here rather than asked of a model.
/// </remarks>
public sealed class ReviewerTests
{
    [Fact] // AGT-14
    public async Task A_pick_that_is_not_the_one_the_tier_dictates_is_caught_without_asking_a_model()
    {
        var judgement = new CountingJudgement([]);
        var reviewer = new Reviewer(judgement);

        // The receipt says the middle tier of the monitor slot is MON-C; the composition says MON-E.
        var verdict = await reviewer.ReviewAsync(
            ASpecification(),
            [AnOption("middle", "MON-E")],
            Receipt());

        verdict.Approved.Should().BeFalse();
        verdict.Findings.Should().ContainSingle().Which.Slot.Should().Be("Monitor");
        judgement.Asked.Should().Be(0, "a computable failure is not a question for a model");
    }

    [Fact] // AGT-14
    public async Task A_slot_the_specification_asked_for_and_the_composition_left_out_is_caught()
    {
        var verdict = await new Reviewer(new CountingJudgement([])).ReviewAsync(
            ASpecification(),
            [new SuggestionOption("middle", [new OptionLine("Monitor", "MON-B", 1)], [], [], [])],
            Receipt());

        verdict.Findings.Should().ContainSingle().Which.Slot.Should().Be("Desk");
    }

    [Fact] // AGT-14
    public async Task A_sound_composition_reaches_the_judgement()
    {
        var judgement = new CountingJudgement([]);

        var verdict = await new Reviewer(judgement).ReviewAsync(
            ASpecification(),
            [AnOption("middle", "MON-B")],
            Receipt());

        verdict.Approved.Should().BeTrue();
        judgement.Asked.Should().Be(1, "nothing computable was wrong, so the judgement is the next question");
    }

    [Fact] // AGT-16
    public async Task A_retry_that_produces_the_specification_it_replaced_stops_the_loop()
    {
        // A budget bounds the cost of trying; it does not oblige the run to spend it on a loop that is
        // not moving. The specification is what a retry could change, so it is what is compared.
        var reviewer = new Reviewer(new CountingJudgement([]));
        var order = Receipt();

        var first = await reviewer.ReviewAsync(ASpecification(), [AnOption("middle", "MON-B")], order);
        var second = await reviewer.ReviewAsync(ASpecification(), [AnOption("middle", "MON-B")], order);

        first.Repeated.Should().BeFalse("nothing has been tried before the first attempt");
        second.Repeated.Should().BeTrue();
    }

    [Fact] // AGT-17
    public async Task The_review_decides_when_there_is_nothing_left_to_try()
    {
        var request = ARequest();

        // Approved is final; so is a retry that repeats itself; so is the third attempt.
        Review(true, request, attempt: 1).IsFinal(3).Should().BeTrue();
        Review(false, request, attempt: 2, repeated: true).IsFinal(3).Should().BeTrue();
        Review(false, request, attempt: 3).IsFinal(3).Should().BeTrue();
        Review(false, request, attempt: 1).IsFinal(3).Should().BeFalse("there is another attempt to spend");
    }

    /// <summary>The receipt for every slot the composition fills: a slot without one reads as unfilled.</summary>
    private static Dictionary<string, IReadOnlyList<string>> Receipt()
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["Monitor"] = ["MON-A", "MON-B", "MON-C"],
            ["Desk"] = ["DSK-A"],
        };

    private static Review Review(bool approved, SuggestionRequest request, int attempt, bool repeated = false)
        => new(
            request,
            ASpecification(),
            [AnOption("middle", "MON-B")],
            approved ? [] : [new Finding("criteria_not_met", "Monitor")],
            approved,
            repeated,
            attempt);

    private static SuggestionOption AnOption(string tier, string monitor)
        => new(
            tier,
            [new OptionLine("Desk", "DSK-A", 1), new OptionLine("Monitor", monitor, 1)],
            [],
            [],
            []);

    private static SuggestionRequest ARequest()
        => new(
            "0f3c4e2a-0000-4000-8000-000000000001",
            "a desk with a monitor",
            [
                new SlotRule("Desk", "Desk", 1, IsMandatory: true),
                new SlotRule("Monitor", "Monitor", 3, IsMandatory: false),
            ]);

    private static Specification ASpecification()
        => new(ARequest(), [new SlotRequirement("Desk", 1, Inferred: false), new SlotRequirement("Monitor", 1, Inferred: false)]);

    /// <summary>The judgement, counted, so a computable failure can be shown not to reach it.</summary>
    private sealed class CountingJudgement(IReadOnlyList<Finding> findings) : IReviewComposition
    {
        public int Asked { get; private set; }

        public Task<IReadOnlyList<Finding>> ReviewAsync(
            string query,
            IReadOnlyList<SuggestionOption> options,
            CancellationToken cancellationToken = default)
        {
            Asked++;

            return Task.FromResult(findings);
        }
    }
}
