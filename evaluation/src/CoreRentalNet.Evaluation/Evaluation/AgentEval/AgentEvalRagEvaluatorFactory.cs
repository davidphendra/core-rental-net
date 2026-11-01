using global::AgentEval.Core;
using global::AgentEval.MAF.Evaluators;
using global::AgentEval.Metrics.RAG;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;

namespace CoreRentalNet.Evaluation.Evaluation.AgentEval;

/// <summary>Builds the AgentEval RAG judge the evaluation report reads.</summary>
public static class AgentEvalRagEvaluatorFactory
{
    public static IAgentEvaluator Create(IChatClient judge)
    {
        ArgumentNullException.ThrowIfNull(judge);

        var metrics = new IMetric[]
        {
            new FaithfulnessMetric(judge),
            new AnswerCorrectnessMetric(judge),
            new ContextPrecisionMetric(judge),
            new ContextRecallMetric(judge),
        };

        return new AgentEvalAgentEvaluator(
            AgentEvalEvaluators.Custom(metrics),
            new ChatConfiguration(judge),
            name: "agenteval-rag");
    }
}
