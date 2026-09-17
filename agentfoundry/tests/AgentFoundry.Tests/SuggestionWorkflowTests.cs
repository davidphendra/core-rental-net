using AgentFoundry.WorkspaceSuggestions.Contracts;
using AgentFoundry.WorkspaceSuggestions.Intent;
using AgentFoundry.WorkspaceSuggestions.Catalogue;
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
/// One run, from the request to the messages it produces.
/// </summary>
/// <remarks>
/// The classifier is hand-written rather than mocked, and it is the only thing in the run that would
/// otherwise need a model: everything asserted here is the contract, the order of the stream, and the
/// decision the verifier makes from what the model said.
/// </remarks>
public sealed class SuggestionWorkflowTests
{
    [Fact] // AGT-01
    public async Task A_request_that_is_not_about_a_workspace_is_refused_with_a_code_and_no_candidates()
    {
        var messages = await CollectAsync(AWorkflow(workspaceRequest: false), ARequest());

        var result = messages.OfType<SuggestionResult>().Should().ContainSingle().Subject;

        result.Status.Should().Be(ReasonCodes.Rejected);
        result.Code.Should().Be(ReasonCodes.NotWorkspaceRequest);
        result.Options.Should().BeEmpty("a refusal answers with a reason, not with a guess");
    }

    [Fact] // AGT-02
    public async Task Every_message_carries_the_run_it_belongs_to_and_the_result_carries_its_status()
    {
        var request = ARequest();
        var messages = await CollectAsync(AWorkflow(workspaceRequest: false), request);

        messages.Should().NotBeEmpty();
        messages.Should().OnlyContain(message => message.RequestId == request.RequestId);

        var result = messages.OfType<SuggestionResult>().Single();
        result.Status.Should().BeOneOf(ReasonCodes.Ok, ReasonCodes.Exhausted, ReasonCodes.Rejected);
        result.Attempts.Should().Be(1);
        result.Kind.Should().Be("result");
    }

    [Fact] // AGT-03
    public async Task The_stage_arrives_before_the_result_and_the_result_arrives_once()
    {
        var messages = await CollectAsync(AWorkflow(workspaceRequest: false), ARequest());

        messages[0].Should().BeOfType<StageEvent>().Which.Stage.Should().Be(Stages.Verifying);
        messages[0].Should().BeOfType<StageEvent>().Which.Attempt.Should().Be(1);
        messages[^1].Should().BeOfType<SuggestionResult>("the result is the terminal message");
        messages.OfType<SuggestionResult>().Should().ContainSingle();
    }

    [Fact] // AGT-03, AGT-17
    public async Task An_accepted_request_passes_every_stage_and_ends_with_a_result()
    {
        var messages = await CollectAsync(AWorkflow(workspaceRequest: true), ARequest());

        messages.OfType<StageEvent>().Select(stage => stage.Stage)
            .Should().Equal(Stages.Verifying, Stages.Rephrasing, Stages.Selecting, Stages.Reviewing);

        var result = messages.OfType<SuggestionResult>().Should().ContainSingle().Subject;
        result.Status.Should().Be(ReasonCodes.Ok);
        result.Attempts.Should().Be(1);
        result.Options.Should().NotBeEmpty();
        result.Findings.Should().BeEmpty();
        messages[^1].Should().BeSameAs(result, "the result is the terminal message");
    }

    [Fact] // AGT-03
    public async Task A_refused_request_never_reaches_the_second_stage()
    {
        var messages = await CollectAsync(AWorkflow(workspaceRequest: false), ARequest());

        messages.OfType<StageEvent>().Select(stage => stage.Stage).Should().Equal(Stages.Verifying);
        messages.OfType<SuggestionResult>().Should().ContainSingle();
    }

    private static SuggestionWorkflow AWorkflow(bool workspaceRequest)
        => new(
            new ScriptedIntentClassifier(workspaceRequest),
            new Rephraser(
                IntentTable.Load(Path.Combine(
                    Repository(), "agentfoundry", "shared", "intent", "workspace-intents.json")),
                new ScriptedSlotClassifier(),
                NullLogger<Rephraser>.Instance),
            new Suggestor(new EverySlotCatalogue()),
            new Reviewer(new NoObjection()));

    /// <summary>
    /// One product per slot, because the query this test uses is a declared phrasing and the table
    /// therefore sends six slots to the catalogue rather than the one the classifier would have.
    /// </summary>
    /// <summary>A judgement that finds nothing wrong, so the graph can reach its end.</summary>
    private sealed class NoObjection : IReviewComposition
    {
        public Task<IReadOnlyList<Finding>> ReviewAsync(
            string query,
            IReadOnlyList<SuggestionOption> options,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Finding>>([]);
    }

    private sealed class EverySlotCatalogue : ICatalogueReader
    {
        private static readonly IReadOnlyList<CatalogueItem> Items =
        [
            new("DSK-A", "A desk", "desk", null, 600, "A desk.", new CatalogueMetadata(["desk"], new Dictionary<string, string>())),
            new("CHA-A", "A chair", "chair", null, 400, "A chair.", new CatalogueMetadata(["chair"], new Dictionary<string, string>())),
            new("MON-A", "A monitor", "accessory", "monitor", 300, "A monitor.", new CatalogueMetadata(["monitor"], new Dictionary<string, string>())),
            new("LMP-A", "A lamp", "accessory", "lamp", 100, "A lamp.", new CatalogueMetadata(["lamp"], new Dictionary<string, string>())),
            new("PLT-A", "A plant", "accessory", "plant", 120, "A plant.", new CatalogueMetadata(["plant"], new Dictionary<string, string>())),
            new("CFE-A", "A coffee station", "accessory", "coffee", 400, "A coffee station.", new CatalogueMetadata(["coffee"], new Dictionary<string, string>())),
        ];

        public Task<CataloguePage> ReadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new CataloguePage(Items, Items.Count, Items.Count, Truncated: false, "IDR"));
    }

    /// <summary>Whatever the table misses on, an empty set is enough for a graph test.</summary>
    private sealed class ScriptedSlotClassifier : ISlotClassifier
    {
        public Task<IReadOnlyList<string>> ClassifyAsync(
            string query,
            IReadOnlyList<SlotRule> slots,
            IReadOnlyList<Finding> findings,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(["Desk"]);
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
            RequestId: "0f3c4e2a-0000-4000-8000-000000000001",
            Query: "a quiet corner for two monitors and a decent chair",
            Slots:
            [
                new SlotRule("Desk", "Desk", 1, IsMandatory: true),
                new SlotRule("Chair", "Chair", 1, IsMandatory: true),
                new SlotRule("Monitor", "Monitor", 3, IsMandatory: false),
            ]);

    private static async Task<List<SuggestionMessage>> CollectAsync(SuggestionWorkflow workflow, SuggestionRequest request)
    {
        var messages = new List<SuggestionMessage>();

        await foreach (var message in workflow.RunAsync(request))
        {
            messages.Add(message);
        }

        return messages;
    }

    /// <summary>The only thing in a run that needs a model, said out loud instead of inferred.</summary>
    private sealed class ScriptedIntentClassifier(bool workspaceRequest) : IIntentClassifier
    {
        public Task<IntentVerdict> ClassifyAsync(string query, CancellationToken cancellationToken = default)
            => Task.FromResult(new IntentVerdict(
                workspaceRequest,
                workspaceRequest ? ReasonCodes.Ok : ReasonCodes.NotWorkspaceRequest));
    }
}
