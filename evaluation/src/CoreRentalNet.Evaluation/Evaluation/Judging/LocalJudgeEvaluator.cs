using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using WorkspaceContext = CoreRentalNet.Evaluation.Evidence.Context.EvaluationContext;

using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;
namespace CoreRentalNet.Evaluation.Evaluation.Judging;

/// <summary>Runs the local llama judge for eligible reranker mismatches.</summary>
public sealed class LocalJudgeEvaluator(
    IChatClient chatClient,
    TimeSpan timeout,
    int retries,
    JudgePromptBuilder promptBuilder,
    JudgeVerdictParser verdictParser) : IAgentEvaluator
{
    public LocalJudgeEvaluator(
        IChatClient chatClient,
        TimeSpan timeout,
        JudgePromptBuilder promptBuilder,
        JudgeVerdictParser verdictParser)
        : this(chatClient, timeout, retries: 1, promptBuilder, verdictParser)
    {
    }

    public string Name => "local-llama-judge";

    public async Task<AgentEvaluationResults> EvaluateAsync(
        IReadOnlyList<EvalItem> items,
        string name,
        CancellationToken cancellationToken)
    {
        var results = new List<EvaluationResult>(items.Count);

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var context = WorkspaceContext.Parse(item.Context);
            var evaluation = await EvaluateItemAsync(context, item.Query, cancellationToken)
                .ConfigureAwait(false);

            results.Add(ToResult(evaluation));
        }

        return new AgentEvaluationResults(Name, results, items);
    }

    private async Task<JudgeEvaluation> EvaluateItemAsync(
        WorkspaceContext context,
        string request,
        CancellationToken cancellationToken)
    {
        if (!ShouldEvaluate(context))
        {
            return JudgeEvaluation.NotEvaluated;
        }

        var lastError = "The judge did not return a verdict.";

        for (var attempt = 1; attempt <= retries + 1; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutSource.CancelAfter(timeout);

                var response = await chatClient.GetResponseAsync(
                        promptBuilder.Build(context, request),
                        new ChatOptions { Temperature = 0 },
                        timeoutSource.Token)
                    .ConfigureAwait(false);

                return verdictParser.Parse(response.Text) with { Attempts = attempt };
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                lastError = "The judge timed out.";
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                lastError = exception.Message;
            }
        }

        return JudgeEvaluation.Error(lastError) with { Attempts = retries + 1 };
    }

    private static bool ShouldEvaluate(WorkspaceContext context)
        => !AnswerComparison.Evaluate(context).Exact;

    private static EvaluationResult ToResult(JudgeEvaluation evaluation)
    {
        var metrics = new List<EvaluationMetric>
        {
            new BooleanMetric(
                MetricNames.JudgeEvaluated,
                evaluation.Evaluated,
                evaluation.Reason),
        };

        if (evaluation.Evaluated)
        {
            metrics.Add(new BooleanMetric(
                MetricNames.JudgePass,
                evaluation.Passed,
                evaluation.Reason));
            metrics.Add(new NumericMetric(
                MetricNames.JudgeConfidence,
                evaluation.Confidence,
                evaluation.Reason));
            metrics.Add(new NumericMetric(
                MetricNames.JudgeAttempts,
                evaluation.Attempts,
                evaluation.Reason));
        }

        return new EvaluationResult(metrics);
    }
}
