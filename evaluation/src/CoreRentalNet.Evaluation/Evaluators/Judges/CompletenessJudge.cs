using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using MseCompleteness = Microsoft.Extensions.AI.Evaluation.Quality.CompletenessEvaluator;
using MseCompletenessContext = Microsoft.Extensions.AI.Evaluation.Quality.CompletenessEvaluatorContext;

using CoreRentalNet.Evaluation.Evaluators.Correctness;
using CoreRentalNet.Evaluation.Evaluators.Shared;
namespace CoreRentalNet.Evaluation.Evaluators.Judges;

/// <summary>Measures whether the answer covers the facts the case states, annotated or derived.</summary>
/// <remarks>
/// The ground truth is the case's own fact list, and its provenance travels with the score. A derived fact list
/// proves the answer covered the requested quantities; it cannot prove it honoured a qualifier nobody wrote down.
/// </remarks>
public sealed class CompletenessJudge(IChatClient judge)
{
    private readonly MseCompleteness _evaluator = new();

    public async Task<JudgeScore> EvaluateAsync(
        RagEvaluationCase testCase,
        JudgeInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(testCase);
        ArgumentNullException.ThrowIfNull(input);

        var facts = testCase.Facts;
        var context = new MseCompletenessContext(string.Join(" ", facts.Facts));

        var result = await _evaluator
            .EvaluateAsync(
                input.Messages,
                input.Response,
                new ChatConfiguration(judge),
                [context],
                cancellationToken)
            .ConfigureAwait(false);

        return JudgeScore.From(result, MseCompleteness.CompletenessMetricName) with
        {
            GroundTruthSource = facts.Source.ToString(),
        };
    }
}
