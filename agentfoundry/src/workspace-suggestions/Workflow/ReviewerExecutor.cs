using AgentFoundry.WorkspaceSuggestions.Review;
using AgentFoundry.WorkspaceSuggestions.Vocabularies;
using Microsoft.Agents.AI.Workflows;

namespace AgentFoundry.WorkspaceSuggestions.Workflows;

/// <summary>
/// The run's last node: it approves the composition or says what is wrong with it.
/// </summary>
/// <remarks>
/// It counts its own attempts, because that is the one thing about a retry that cannot be derived from
/// the composition - the same specification arriving twice is exactly the case the count exists to
/// stop. The count it reports is the attempt it just reviewed; the retry it asks for is the next one.
/// </remarks>
internal sealed class ReviewerExecutor(IReviewCandidates reviewer)
    : Executor<Candidates, Review>(Stages.Reviewing)
{
    private int _reviewed;

    public override async ValueTask<Review> HandleAsync(
        Candidates message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var attempt = ++_reviewed;
        var verdict = await reviewer.ReviewAsync(
            message.Specification,
            message.Options,
            message.Ordered,
            cancellationToken);

        return new Review(
            message.Specification.Request,
            message.Specification,
            message.Options,
            verdict.Findings,
            verdict.Approved,
            verdict.Repeated,
            attempt);
    }
}
