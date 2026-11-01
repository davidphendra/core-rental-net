using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Projection;
using CoreRentalNet.Evaluation.Evidence.Context;
using CoreRentalNet.Evaluation.Execution.Transport;
using CoreRentalNet.Evaluation.Execution.Auth;
using CoreRentalNet.Evaluation.Execution.Tools;
using CoreRentalNet.Evaluation.Execution.Running;

using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Assembly;
namespace CoreRentalNet.Evaluation.Runs.Result;

/// <summary>Projects an executed row into the stage-separated result the metrics consume.</summary>
/// <remarks>
/// The projection reads the same <see cref="EvaluationContext"/> every existing check reads, so the new metrics
/// and the established ones can never disagree about what a run contained.
/// </remarks>
public sealed class RowRunRagProjector(EvaluationContextFactory contextFactory)
{
    public RagRunResult Project(RowRun run)
    {
        ArgumentNullException.ThrowIfNull(run);

        var context = contextFactory.Create(run);

        return new RagRunResult
        {
            CaseId = run.Case.CaseId,
            Query = run.Case.Sentence,
            RetrievedSkus = [.. context.FinalToolAnswers.SelectMany(answer => answer.ReturnedSkus)],
            RetrievalPasses =
            [
                .. context.FinalToolAnswers.Select(answer => new RagRetrievalPass(answer.ToolName, answer.ReturnedSkus)),
            ],
            AnswerText = Render(context),
            RerankedSkus = [.. context.FinalRerankSelections.Select(selection => selection.Sku)],
            RecommendedLines = [.. Lines(context)],
            Outcome = OutcomeExtractor.From(context.RunStatus),
            RunStatus = context.RunStatus,
            Completed = run.Completed,
            TimedOut = run.TimedOut,
            WallMs = run.WallMs,
        };
    }

    /// <summary>Renders the recommendation the way the agent reports it, so a judge sees the answer itself.</summary>
    private static string Render(EvaluationContext context)
    {
        var lines = Lines(context)
            .Select(line => $"{line.Slot}: {line.Sku} x{line.Quantity}")
            .ToArray();

        return lines.Length == 0
            ? $"Outcome: {context.RunStatus}. No workspace was recommended."
            : $"Outcome: {context.RunStatus}. Recommended workspace: {string.Join("; ", lines)}.";
    }

    private static IEnumerable<RagSuggestedLine> Lines(EvaluationContext context)
        => context.Candidates
            .SelectMany(candidate => candidate.Lines)
            .Select(line => new RagSuggestedLine(line.Slot, line.Sku, line.Quantity));
}
