using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using MseRelevance = Microsoft.Extensions.AI.Evaluation.Quality.RelevanceEvaluator;

using CoreRentalNet.Evaluation.Evaluators.Correctness;
using CoreRentalNet.Evaluation.Evaluators.Shared;
namespace CoreRentalNet.Evaluation.Evaluators.Judges;

/// <summary>Measures whether the answer addresses the request, using the Microsoft relevance evaluator.</summary>
/// <remarks>
/// The metric name comes from the evaluator's own constant rather than a literal, so a rename in the package is a
/// compile error here instead of a silently mismatched metric key in the report.
/// </remarks>
public sealed class RelevanceJudge(IChatClient judge)
{
    private readonly MseRelevance _evaluator = new();

    public async Task<JudgeScore> EvaluateAsync(JudgeInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var result = await _evaluator
            .EvaluateAsync(
                input.Messages,
                input.Response,
                new ChatConfiguration(judge),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return JudgeScore.From(result, MseRelevance.RelevanceMetricName);
    }
}
