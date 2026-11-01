using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;
using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Projection;
using CoreRentalNet.Evaluation.Evidence.Context;
using Microsoft.Agents.AI;

using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;

namespace CoreRentalNet.Evaluation.Evaluation.AgentEval;

/// <summary>Projects one workspace-evidence item into the inputs AgentEval's RAG metrics expect.</summary>
public sealed class AgentEvalRagContextProjector
{
    public EvalItem Project(EvalItem item)
    {
        var context = EvaluationContext.Parse(item.Context);
        var best = BestVariant(context);

        return new EvalItem(item.Conversation, ConversationSplitters.LastTurn)
        {
            RawResponse = item.RawResponse,
            Tools = item.Tools,
            Context = AgentEvalRetrievedEvidence.Render(context),
            ExpectedOutput = best is null ? string.Empty : EvaluationText.Expected([best]),
        };
    }

    private static ExpectedSetupVariant? BestVariant(EvaluationContext context)
        => context.ExpectedVariants.Count == 0
            ? null
            : context.ExpectedVariants
                .OrderByDescending(variant => variant.F1(context.Candidates))
                .First();
}
