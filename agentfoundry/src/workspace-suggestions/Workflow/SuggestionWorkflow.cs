using System.Runtime.CompilerServices;
using AgentFoundry.WorkspaceSuggestions.Contracts;
using AgentFoundry.WorkspaceSuggestions.Intent;
using AgentFoundry.WorkspaceSuggestions.Review;
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
/// The retry edge runs from the reviewer back to the rephraser, not to the verifier: the request was
/// already judged to be about a workspace, and what changes between attempts is the specification, not
/// the question. The loop is bounded, and a retry that produces the specification it replaced stops it
/// early - a budget bounds the cost of trying, it does not oblige the run to try pointlessly.
/// </para>
/// <para>
/// A refusal is a result, not an exception. The verifier succeeding at its job is what produces it, so
/// the caller receives a typed answer carrying a code, and the transport stays a success either way.
/// </para>
/// </remarks>
public sealed class SuggestionWorkflow
{
    /// <summary>How many times the composition may be sent back to be rephrased.</summary>
    public const int Attempts = 3;

    private readonly IIntentClassifier _classifier;
    private readonly IRephraseRequests _rephraser;
    private readonly ISelectCandidates _suggestor;
    private readonly IReviewCandidates _reviewer;

    public SuggestionWorkflow(
        IIntentClassifier classifier,
        IRephraseRequests rephraser,
        ISelectCandidates suggestor,
        IReviewCandidates reviewer)
    {
        ArgumentNullException.ThrowIfNull(classifier);
        ArgumentNullException.ThrowIfNull(rephraser);
        ArgumentNullException.ThrowIfNull(suggestor);
        ArgumentNullException.ThrowIfNull(reviewer);

        _classifier = classifier;
        _rephraser = rephraser;
        _suggestor = suggestor;
        _reviewer = reviewer;
    }

    /// <summary>Runs one request, streaming the stages it passes and the result it ends with.</summary>
    public async IAsyncEnumerable<SuggestionMessage> RunAsync(
        SuggestionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var run = await InProcessExecution.RunStreamingAsync(Build(), request);

        Round? last = null;
        Exception? failure = null;
        var attempt = 1;
        var reviewed = false;

        await foreach (var raised in run.WatchStreamAsync(cancellationToken))
        {
            // A node that throws ends the run the same way a short one does. Left unread, a failure
            // inside the graph would look like a request that simply produced no result.
            if (raised is WorkflowErrorEvent { Exception: { } error })
            {
                failure = error;
            }
            else if (raised is ExecutorCompletedEvent { ExecutorId: var stage } && Stages.InOrder.Contains(stage))
            {
                // The rephraser is the first node of every attempt after the first, so it is the
                // boundary between them.
                if (stage == Stages.Rephrasing && reviewed)
                {
                    attempt++;
                }

                if (stage == Stages.Reviewing)
                {
                    reviewed = true;
                }

                yield return Stage(request, stage, attempt);
            }
            else if (raised is WorkflowOutputEvent { Data: Round answered })
            {
                last = answered;
            }
        }

        if (failure is not null)
        {
            throw new InvalidOperationException(
                $"The workflow failed for request '{request.RequestId}'.", failure);
        }

        if (last is null)
        {
            throw new InvalidOperationException(
                $"The workflow ended without answering request '{request.RequestId}'.");
        }

        if (!last.Verdict.IsWorkspaceRequest)
        {
            yield return new SuggestionResult
            {
                RequestId = request.RequestId,
                Status = ReasonCodes.Rejected,
                Attempts = 1,
                Code = last.Verdict.Code,
            };

            yield break;
        }

        if (last.Specification is null)
        {
            throw new InvalidOperationException(
                $"The workflow composed a workspace for request '{request.RequestId}' and nothing judged it.");
        }

        // Exhausted is a result, not a failure: the candidates travel with the findings against them,
        // so the application can show what it has and say what could not be confirmed.
        yield return new SuggestionResult
        {
            RequestId = request.RequestId,
            Status = last.Approved ? ReasonCodes.Ok : ReasonCodes.Exhausted,
            Attempts = last.Attempt,
            Options = last.Options,
            Findings = last.Findings,
        };
    }

    private Workflow Build()
    {
        var verifier = new VerifierExecutor(_classifier);
        var rephraser = new RephraserExecutor(_rephraser);
        var suggestor = new SuggestorExecutor(_suggestor);
        var reviewer = new ReviewerExecutor(_reviewer);

        return new WorkflowBuilder(verifier)
            .AddEdge<Round>(
                verifier,
                rephraser,
                condition: raised => raised is { Verdict.IsWorkspaceRequest: true })
            .AddEdge(rephraser, suggestor)
            .AddEdge(suggestor, reviewer)
            .AddEdge<Round>(
                reviewer,
                rephraser,
                condition: raised => raised is Round round && !round.IsFinal(Attempts))
            .WithOutputFrom(verifier, reviewer)
            .Build();
    }

    private static StageEvent Stage(SuggestionRequest request, string stage, int attempt)
        => new()
        {
            RequestId = request.RequestId,
            Stage = stage,
            Attempt = attempt,
        };
}
