using System.Runtime.CompilerServices;
using AgentFoundry.WorkspaceSuggestions.Contracts;
using AgentFoundry.WorkspaceSuggestions.Intent;
using AgentFoundry.WorkspaceSuggestions.Selection;
using AgentFoundry.WorkspaceSuggestions.Specifications;
using AgentFoundry.WorkspaceSuggestions.Vocabularies;
using Microsoft.Agents.AI.Workflows;

namespace AgentFoundry.WorkspaceSuggestions.Workflows;

/// <summary>
/// One run: a request in, and the messages it produces in the order it produced them.
/// </summary>
/// <remarks>
/// <para>
/// The graph is built with Microsoft Agent Framework, and the run is streamed: the caller sees each
/// stage as the node behind it finishes rather than waiting for the end, which is the whole reason the
/// contract has stage messages at all. Each node's executor id <em>is</em> its stage id, so the event
/// the framework raises is already the message the contract sends - one name, not a name and a mapping
/// that can disagree with it.
/// </para>
/// <para>
/// A refusal is a result, not an exception. The verifier succeeding at its job is what produces it, so
/// the caller receives a typed answer carrying a code, and the transport stays a success either way.
/// </para>
/// </remarks>
public sealed class SuggestionWorkflow
{
    /// <summary>Which attempt every stage belongs to. Retries arrive with the reviewer.</summary>
    private const int FirstAttempt = 1;

    private readonly IIntentClassifier _classifier;
    private readonly IRephraseRequests _rephraser;
    private readonly ISelectCandidates _suggestor;

    public SuggestionWorkflow(
        IIntentClassifier classifier,
        IRephraseRequests rephraser,
        ISelectCandidates suggestor)
    {
        ArgumentNullException.ThrowIfNull(classifier);
        ArgumentNullException.ThrowIfNull(rephraser);
        ArgumentNullException.ThrowIfNull(suggestor);

        _classifier = classifier;
        _rephraser = rephraser;
        _suggestor = suggestor;
    }

    /// <summary>Runs one request, streaming the stages it passes and the result it ends with.</summary>
    /// <remarks>
    /// A request the verifier accepts currently ends after the candidates: the reviewer that approves
    /// them attaches to this graph as it is written, and until it exists there is no honest result to
    /// send - <c>ok</c> means a reviewer approved the composition, and nothing has.
    /// </remarks>
    public async IAsyncEnumerable<SuggestionMessage> RunAsync(
        SuggestionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var verifier = new VerifierExecutor(_classifier);
        var rephraser = new RephraserExecutor(_rephraser);
        var suggestor = new SuggestorExecutor(_suggestor);

        var workflow = new WorkflowBuilder(verifier)
            .AddEdge<Verification>(
                verifier,
                rephraser,
                condition: raised => raised is { Verdict.IsWorkspaceRequest: true })
            .AddEdge(rephraser, suggestor)
            .WithOutputFrom(verifier)
            .Build();

        await using var run = await InProcessExecution.RunStreamingAsync(workflow, request);

        Verification? verification = null;
        Exception? failure = null;

        await foreach (var raised in run.WatchStreamAsync(cancellationToken))
        {
            // A node that throws ends the run the same way a short one does. Left unread, a failure
            // inside the graph would look like a request that simply produced no result.
            if (raised is WorkflowErrorEvent { Exception: { } error })
            {
                failure = error;
            }

            if (raised is ExecutorCompletedEvent { ExecutorId: var stage } && Stages.InOrder.Contains(stage))
            {
                yield return Stage(request, stage);
            }
            else if (raised is WorkflowOutputEvent { Data: Verification answer })
            {
                verification = answer;
            }
        }

        if (failure is not null)
        {
            throw new InvalidOperationException(
                $"The workflow failed for request '{request.RequestId}'.", failure);
        }

        if (verification is null)
        {
            throw new InvalidOperationException(
                $"The workflow ended without answering request '{request.RequestId}'.");
        }

        if (!verification.Verdict.IsWorkspaceRequest)
        {
            yield return new SuggestionResult
            {
                RequestId = request.RequestId,
                Status = ReasonCodes.Rejected,
                Attempts = FirstAttempt,
                Code = verification.Verdict.Code,
            };
        }
    }

    private static StageEvent Stage(SuggestionRequest request, string stage)
        => new()
        {
            RequestId = request.RequestId,
            Stage = stage,
            Attempt = FirstAttempt,
        };
}
