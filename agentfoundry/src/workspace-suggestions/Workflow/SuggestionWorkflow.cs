using System.Runtime.CompilerServices;
using AgentFoundry.WorkspaceSuggestions.Contracts;
using AgentFoundry.WorkspaceSuggestions.Intent;
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
/// contract has stage messages at all.
/// </para>
/// <para>
/// A refusal is a result, not an exception. The verifier succeeding at its job is what produces it, so
/// the caller receives a typed answer carrying a code, and the transport stays a success either way.
/// </para>
/// </remarks>
public sealed class SuggestionWorkflow
{
    private readonly IIntentClassifier _classifier;

    public SuggestionWorkflow(IIntentClassifier classifier)
    {
        ArgumentNullException.ThrowIfNull(classifier);

        _classifier = classifier;
    }

    /// <summary>Runs one request, streaming the stages it passes and the result it ends with.</summary>
    /// <remarks>
    /// A request the verifier accepts currently ends after verification: the nodes that read the
    /// catalogue and compose candidates attach to this graph as they are written, and until they exist
    /// there is no honest result to send - an approved composition with no products in it would be a
    /// lie the caller could not detect.
    /// </remarks>
    public async IAsyncEnumerable<SuggestionMessage> RunAsync(
        SuggestionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        yield return Stage(request, Stages.Verifying, attempt: 1);

        var verification = await VerifyAsync(request, cancellationToken);

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
                Attempts = 1,
                Code = verification.Verdict.Code,
            };
        }
    }

    private async Task<Verification?> VerifyAsync(SuggestionRequest request, CancellationToken cancellationToken)
    {
        var verifier = new VerifierExecutor(_classifier);
        var workflow = new WorkflowBuilder(verifier).WithOutputFrom(verifier).Build();

        await using var run = await InProcessExecution.RunStreamingAsync(workflow, request);

        Verification? verification = null;

        await foreach (var raised in run.WatchStreamAsync(cancellationToken))
        {
            if (raised is WorkflowOutputEvent { Data: Verification answer })
            {
                verification = answer;
            }
        }

        return verification;
    }

    private static StageEvent Stage(SuggestionRequest request, string stage, int attempt)
        => new()
        {
            RequestId = request.RequestId,
            Stage = stage,
            Attempt = attempt,
        };
}
