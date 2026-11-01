using System.Text.Json;

namespace CoreRentalNet.Evaluation.Configuration;

/// <summary>The pass line per metric, read from configuration so no threshold is baked into code.</summary>
/// <remarks>
/// A metric with no configured minimum is reported with its score and no verdict. That is deliberate: an
/// invented default would turn an opinion about a good score into a failed build.
/// </remarks>
public sealed record EvaluationThresholds(IReadOnlyDictionary<string, double> Minimums)
{
    public static EvaluationThresholds None { get; } =
        new(new Dictionary<string, double>(StringComparer.Ordinal));

    public static EvaluationThresholds Load(string path)
    {
        if (!File.Exists(path))
        {
            return None;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));

        if (!document.RootElement.TryGetProperty("Evaluation", out var evaluation)
            || !evaluation.TryGetProperty("Metrics", out var metrics))
        {
            return None;
        }

        var minimums = new Dictionary<string, double>(StringComparer.Ordinal);

        foreach (var metric in metrics.EnumerateObject())
        {
            if (metric.Value.TryGetProperty("RequiredMinimum", out var minimum)
                && minimum.TryGetDouble(out var value))
            {
                minimums[metric.Name] = value;
            }
        }

        return new EvaluationThresholds(minimums);
    }
}
