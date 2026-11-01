using AwesomeAssertions;
using CoreRentalNet.Evaluation.Commands;
using CoreRentalNet.Evaluation.Configuration;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests.Execution;

public sealed class RunEvaluationOptionsParserTests
{
    [Fact]
    public void The_judge_mode_threshold_and_exports_are_read()
    {
        var options = RunEvaluationOptionsParser.Parse(
        [
            "--judge-mode", "both",
            "--success-rate-threshold", "0.9",
            "--export", "markdown,junit",
        ]);

        options.JudgeMode.Should().Be(JudgeMode.Both);
        options.SuccessRateThreshold.Should().Be(0.9);
        options.Exports.Should().Equal("markdown", "junit");
    }

    [Fact]
    public void An_unknown_judge_mode_is_refused()
    {
        var act = () => RunEvaluationOptionsParser.Parse(["--judge-mode", "invalid"]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void A_threshold_outside_zero_to_one_is_refused()
    {
        var act = () => RunEvaluationOptionsParser.Parse(["--success-rate-threshold", "1.1"]);

        act.Should().Throw<ArgumentException>();
    }
}
