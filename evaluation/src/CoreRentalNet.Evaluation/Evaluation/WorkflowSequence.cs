using CoreRentalNet.Evaluation.Evidence;

namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>Validates the successful workflow, including the retry path back to rephrasing.</summary>
public static class WorkflowSequence
{
    private const string Verifying = "verifyingRequest";
    private const string Rephrasing = "rephrasingRequirement";
    private const string Retrieving = "retrievingCatalogueProducts";
    private const string Reranking = "rerankingWorkspaceCandidates";
    private const string Composing = "composingWorkspaceSetups";

    private static readonly string[] AttemptPrefix =
        [Rephrasing, Retrieving, Reranking, Composing];

    public static WorkflowResult Evaluate(EvaluationContext context)
    {
        var failures = new List<string>();

        if (!string.Equals(context.RunStatus, "success", StringComparison.Ordinal))
        {
            failures.Add(FailureCodes.WorkflowStatus + ":" + context.RunStatus);
        }

        if (!context.CompletedIsLast)
        {
            failures.Add(FailureCodes.WorkflowCompletedNotLast);
        }

        if (context.Candidates.Count == 0)
        {
            failures.Add(FailureCodes.WorkflowNoCandidate);
        }

        if (context.Stages.Count == 0 || !string.Equals(context.Stages[0], Verifying, StringComparison.Ordinal))
        {
            failures.Add(FailureCodes.WorkflowSequence);
            return new WorkflowResult(false, failures.Distinct(StringComparer.Ordinal).ToArray());
        }

        var attempts = SplitAttempts(context.Stages.Skip(1).ToArray());

        if (attempts.Count == 0 || attempts.Count > 3)
        {
            failures.Add(FailureCodes.WorkflowSequence);
        }

        for (var index = 0; index < attempts.Count; index++)
        {
            var attempt = attempts[index];
            var isLast = index == attempts.Count - 1;

            if (attempt.Length != AttemptPrefix.Length
                || !attempt.SequenceEqual(AttemptPrefix, StringComparer.Ordinal))
            {
                failures.Add(FailureCodes.WorkflowSequence);
                continue;
            }

            if (isLast && !string.Equals(attempt[^1], Composing, StringComparison.Ordinal))
            {
                failures.Add(FailureCodes.WorkflowSequence);
            }
        }

        return new WorkflowResult(
            failures.Count == 0,
            failures.Distinct(StringComparer.Ordinal).ToArray());
    }

    private static List<string[]> SplitAttempts(string[] stages)
    {
        var attempts = new List<string[]>();
        var current = new List<string>();

        foreach (var stage in stages)
        {
            if (string.Equals(stage, Rephrasing, StringComparison.Ordinal) && current.Count > 0)
            {
                attempts.Add([.. current]);
                current.Clear();
            }

            current.Add(stage);
        }

        if (current.Count > 0)
        {
            attempts.Add([.. current]);
        }

        return attempts;
    }
}
