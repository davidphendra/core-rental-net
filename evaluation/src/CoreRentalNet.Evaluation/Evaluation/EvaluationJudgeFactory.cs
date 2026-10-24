using CoreRentalNet.Evaluation.Configuration;
using Microsoft.Agents.AI;

namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>Creates the optional local judge from configuration.</summary>
public static class EvaluationJudgeFactory
{
    public static IAgentEvaluator? Create(EvaluationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.JudgeEndpoint is null)
        {
            return null;
        }

        var chatClient = OpenAiCompatibleJudgeClientFactory.Create(
            options.JudgeEndpoint,
            options.JudgeApiKey,
            options.JudgeModel);

        return new LocalJudgeEvaluator(
            chatClient,
            options.JudgeTimeout,
            new JudgePromptBuilder(),
            new JudgeVerdictParser());
    }
}
