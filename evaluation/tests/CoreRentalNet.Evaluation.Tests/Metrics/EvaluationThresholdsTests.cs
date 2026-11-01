using AwesomeAssertions;
using CoreRentalNet.Evaluation.Configuration;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests.Metrics;

/// <summary>Thresholds come from configuration, and an absent file means no pass line at all.</summary>
public sealed class EvaluationThresholdsTests
{
    [Fact]
    public void A_missing_file_configures_nothing()
        => EvaluationThresholds.Load(Path.Combine(Path.GetTempPath(), "no-such-thresholds.json")).Minimums.Should().BeEmpty();

    [Fact]
    public void A_configured_minimum_is_read()
    {
        var path = Write("""{ "Evaluation": { "Metrics": { "outcome_accuracy": { "Enabled": true, "RequiredMinimum": 0.85 } } } }""");

        try
        {
            EvaluationThresholds.Load(path).Minimums["outcome_accuracy"].Should().Be(0.85);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void A_metric_without_a_minimum_has_no_pass_line()
    {
        var path = Write("""{ "Evaluation": { "Metrics": { "answer_correctness": { "Enabled": true } } } }""");

        try
        {
            EvaluationThresholds.Load(path).Minimums.Should().NotContainKey("answer_correctness");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string Write(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"thresholds-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }
}
