using CoreRentalNet.Evaluation.Configuration;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;
namespace CoreRentalNet.Evaluation.Evaluation.Judging;

/// <summary>Creates the optional local judge from configuration.</summary>
public static class EvaluationJudgeFactory
{
    /// <summary>The judge chat client, or null when no judge endpoint is configured.</summary>
    public static IChatClient? CreateClient(EvaluationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.JudgeEndpoint is null
            ? null
            : OpenAiCompatibleJudgeClientFactory.Create(
                options.JudgeEndpoint,
                options.JudgeApiKey,
                options.JudgeModel);
    }

    /// <summary>The domain equivalence judge.</summary>
    public static IAgentEvaluator CreateEquivalenceJudge(
        IChatClient chatClient,
        TimeSpan timeout,
        int retries)
    {
        ArgumentNullException.ThrowIfNull(chatClient);

        return new LocalJudgeEvaluator(
            chatClient,
            timeout,
            retries,
            new JudgePromptBuilder(),
            new JudgeVerdictParser());
    }

    /// <summary>The equivalence judge, or null when no judge endpoint is configured.</summary>
    public static IAgentEvaluator? Create(EvaluationOptions options)
    {
        var client = CreateClient(options);

        return client is null
            ? null
            : CreateEquivalenceJudge(client, options.JudgeTimeout, options.JudgeRetries);
    }
}
