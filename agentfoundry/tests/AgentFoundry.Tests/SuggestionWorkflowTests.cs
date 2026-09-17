using AgentFoundry.WorkspaceSuggestions.Contracts;
using AgentFoundry.WorkspaceSuggestions.Intent;
using AgentFoundry.WorkspaceSuggestions.Vocabularies;
using AgentFoundry.WorkspaceSuggestions.Workflows;
using AwesomeAssertions;
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

    [Fact] // AGT-03
    public async Task An_accepted_request_streams_its_stage_and_sends_no_result_yet()
    {
        // Not a passing state to ship, but the honest one: the nodes that read the catalogue and compose
        // candidates attach to this graph as they are written, and an approved composition with no
        // products in it would be a result the caller could not tell from a real one.
        var messages = await CollectAsync(AWorkflow(workspaceRequest: true), ARequest());

        messages.Should().ContainSingle().Which.Should().BeOfType<StageEvent>();
    }

    private static SuggestionWorkflow AWorkflow(bool workspaceRequest)
        => new(new ScriptedIntentClassifier(workspaceRequest));

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
