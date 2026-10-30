using AgentEval.Core;
using AgentEval.MAF.Evaluators;
using AgentEval.Metrics.RAG;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

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
