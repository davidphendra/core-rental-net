using System.Security.Cryptography;
using System.Text;
using CoreRentalNet.Evaluation.Configuration;

using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;
namespace CoreRentalNet.Evaluation.Evaluation.Pipeline;

/// <summary>The evaluation policy identity two runs must share before their scores are compared.</summary>
public static class EvaluationPolicy
{
    public const string Version = "workspace-suggestion-eval/1";

    /// <summary>The digest for a run with no judge configured.</summary>
    public static string DeterministicDigest { get; } = Compute("none");

    /// <summary>The digest for one judge mode, so a RAG run and an equivalence-only run are not comparable.</summary>
    public static string Digest(JudgeMode judgeMode) => Compute(judgeMode.ToString());

    private static string Compute(string instrument)
    {
        var material = string.Join(
            "|",
            Version,
            instrument,
            string.Join(",", MetricNames.All),
            string.Join(",", MetricNames.RagMetrics),
            "bar=70");

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(material));

        return "sha256:" + Convert.ToHexString(digest).ToLowerInvariant();
    }
}
