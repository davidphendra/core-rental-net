using System.Text.Json;
using CoreRentalNet.Evaluation.Evidence;
using CoreRentalNet.Evaluation.Execution;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace CoreRentalNet.Evaluation.Evaluation;

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
