using System.Text.Json;
using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Projection;
using CoreRentalNet.Evaluation.Evidence.Context;
using CoreRentalNet.Evaluation.Execution.Transport;
using CoreRentalNet.Evaluation.Execution.Auth;
using CoreRentalNet.Evaluation.Execution.Tools;
using CoreRentalNet.Evaluation.Execution.Running;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;
namespace CoreRentalNet.Evaluation.Evaluation.Pipeline;

/// <summary>Builds the trace-enriched item the built-in evaluators score.</summary>
public sealed class EvalItemFactory(
    IReadOnlyList<AITool> tools,
    EvaluationContextFactory contextFactory)
{
    public IReadOnlyList<EvalItem> Create(IReadOnlyList<RowRun> runs)
        => [.. runs.Select(Create)];

    private EvalItem Create(RowRun run)
    {
        var context = contextFactory.Create(run);
        var conversation = EvalConversationFactory.Create(context, run.Case.Sentence);

        return new EvalItem(conversation, ConversationSplitters.LastTurn)
        {
            RawResponse = new ChatResponse(conversation.Skip(1).ToList()),
            Tools = tools,
            Context = JsonSerializer.Serialize(context, EvaluationJson.Options),
            ExpectedOutput = EvaluationText.Expected(context.ExpectedVariants),
        };
    }
}
