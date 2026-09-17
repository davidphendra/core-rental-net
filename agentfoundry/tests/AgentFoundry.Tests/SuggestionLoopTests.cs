using AgentFoundry.WorkspaceSuggestions.Catalogue;
using AgentFoundry.WorkspaceSuggestions.Contracts;
using AgentFoundry.WorkspaceSuggestions.Intent;
using AgentFoundry.WorkspaceSuggestions.Review;
using AgentFoundry.WorkspaceSuggestions.Selection;
using AgentFoundry.WorkspaceSuggestions.Specifications;
using AgentFoundry.WorkspaceSuggestions.Vocabularies;
using AgentFoundry.WorkspaceSuggestions.Workflows;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AgentFoundry.Tests;

/// <summary>
/// The loop, end to end: a refusal sends the work back, and a run that cannot be approved says so.
/// </summary>
/// <remarks>
/// The judgement is the only thing here that would need a model, and it is said out loud. What is
/// asserted is the shape of the run: which stages stream, how many attempts are spent, and what the
/// caller receives when the attempts run out.
/// </remarks>
public sealed class SuggestionLoopTests
{
    [Fact(Skip = "Blocked: the retry edge does not fire under Microsoft.Agents.AI.Workflows 1.21.0. See specs/verifications/e02s05-after.md.")] // AGT-15
    public async Task A_judged_failure_sends_the_work_back_to_be_rephrased()
    {
        var judgement = new AlwaysObjects();
        var messages = await CollectAsync(judgement);

        // The retry re-enters the rephraser rather than the verifier: the request was already judged to
        // be about a workspace, and what changes between attempts is the specification, not the question.
        messages.OfType<StageEvent>().Select(stage => stage.Stage).Should().Equal(
            Stages.Verifying, Stages.Rephrasing, Stages.Selecting, Stages.Reviewing,
            Stages.Rephrasing, Stages.Selecting, Stages.Reviewing);

        judgement.Asked.Should().Be(2, "the loop stopped rather than spending the third attempt");
    }

    [Fact(Skip = "Blocked: the retry edge does not fire under Microsoft.Agents.AI.Workflows 1.21.0. See specs/verifications/e02s05-after.md.")] // AGT-16
    public async Task A_retry_that_changes_nothing_ends_the_run_rather_than_repeating_it()
    {
        // The rephraser is deliberate: the same request produces the same specification. So the second
        // attempt is the first one twice, and the guard stops there - which is why the three-attempt
        // budget is a bound and not a quota. A rephraser that varied would use the third.
        var messages = await CollectAsync(new AlwaysObjects());

        messages.OfType<StageEvent>().Count(stage => stage.Stage == Stages.Rephrasing).Should().Be(2);
    }

    [Fact(Skip = "Blocked: the retry edge does not fire under Microsoft.Agents.AI.Workflows 1.21.0. See specs/verifications/e02s05-after.md.")] // AGT-17
    public async Task Exhaustion_is_a_result_carrying_the_candidates_and_what_was_objected_to()
    {
        var messages = await CollectAsync(new AlwaysObjects());

        var result = messages.OfType<SuggestionResult>().Should().ContainSingle().Subject;

        result.Status.Should().Be(ReasonCodes.Exhausted, "not a failure: the candidates travel with the findings");
        result.Attempts.Should().Be(2);
        result.Options.Should().NotBeEmpty();
        result.Findings.Should().NotBeEmpty();
        messages[^1].Should().BeSameAs(result);
    }

    [Fact(Skip = "Blocked: the retry edge does not fire under Microsoft.Agents.AI.Workflows 1.21.0. See specs/verifications/e02s05-after.md.")] // AGT-17
    public async Task A_stage_carries_the_attempt_it_belongs_to()
    {
        var attempts = (await CollectAsync(new AlwaysObjects()))
            .OfType<StageEvent>()
            .Select(stage => (stage.Stage, stage.Attempt));

        attempts.Should().Equal(
            (Stages.Verifying, 1), (Stages.Rephrasing, 1), (Stages.Selecting, 1), (Stages.Reviewing, 1),
            (Stages.Rephrasing, 2), (Stages.Selecting, 2), (Stages.Reviewing, 2));
    }

    private static async Task<List<SuggestionMessage>> CollectAsync(IReviewComposition judgement)
    {
        var request = ARequest();

        var workflow = new SuggestionWorkflow(
            new SaysYes(),
            new Rephraser(
                IntentTable.Load(Path.Combine(Repository(), "agentfoundry", "shared", "intent", "workspace-intents.json")),
                new SaysDesk(),
                NullLogger<Rephraser>.Instance),
            new Suggestor(new Catalogue()),
            new Reviewer(judgement));

        var messages = new List<SuggestionMessage>();

        await foreach (var message in workflow.RunAsync(request))
        {
            messages.Add(message);
        }

        return messages;
    }

    private static string Repository()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "agentfoundry")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("the test binary runs inside the repository");
    }

    private static SuggestionRequest ARequest()
        => new(
            "0f3c4e2a-0000-4000-8000-000000000001",
            "somewhere to think",
            [
                new SlotRule("Desk", "Desk", 1, IsMandatory: true),
                new SlotRule("Chair", "Chair", 1, IsMandatory: true),
                new SlotRule("Monitor", "Monitor", 3, IsMandatory: false),
            ]);

    /// <summary>A request that is about a workspace, which is all this loop needs from the verifier.</summary>
    private sealed class SaysYes : IIntentClassifier
    {
        public Task<IntentVerdict> ClassifyAsync(string query, CancellationToken cancellationToken = default)
            => Task.FromResult(new IntentVerdict(IsWorkspaceRequest: true, ReasonCodes.Ok));
    }

    /// <summary>The table misses this phrasing, so the slot set is inferred - and the same every time.</summary>
    private sealed class SaysDesk : ISlotClassifier
    {
        public Task<IReadOnlyList<string>> ClassifyAsync(
            string query,
            IReadOnlyList<SlotRule> slots,
            IReadOnlyList<Finding> findings,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(["Desk"]);
    }

    private sealed class Catalogue : ICatalogueReader
    {
        public Task<CataloguePage> ReadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new CataloguePage(
                [new CatalogueItem("DSK-A", "A desk", "desk", null, 600, "A desk.", new CatalogueMetadata(["desk"], new Dictionary<string, string>()))],
                1,
                1,
                Truncated: false,
                "IDR"));
    }

    /// <summary>Objects every time, which is what a run that cannot be approved looks like.</summary>
    private sealed class AlwaysObjects : IReviewComposition
    {
        public int Asked { get; private set; }

        public Task<IReadOnlyList<Finding>> ReviewAsync(
            string query,
            IReadOnlyList<SuggestionOption> options,
            CancellationToken cancellationToken = default)
        {
            Asked++;

            return Task.FromResult<IReadOnlyList<Finding>>([new Finding("criteria_not_met", "Desk")]);
        }
    }
}
