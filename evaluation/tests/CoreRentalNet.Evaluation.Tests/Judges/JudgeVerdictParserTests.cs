using AwesomeAssertions;
using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests.Judges;

public sealed class JudgeVerdictParserTests
{
    private readonly JudgeVerdictParser _parser = new();

    [Fact]
    public void Parses_a_pass_verdict()
    {
        var result = _parser.Parse("""{"verdict":"pass","confidence":0.91,"reason":"Equivalent."}""");

        result.Passed.Should().BeTrue();
        result.Confidence.Should().Be(0.91);
    }

    [Fact]
    public void Parses_a_fenced_fail_verdict()
    {
        var result = _parser.Parse("```json\n{\"verdict\":\"fail\",\"confidence\":0.8,\"reason\":\"Wrong size.\"}\n```");

        result.Passed.Should().BeFalse();
        result.Reason.Should().Be("Wrong size.");
    }

    [Fact]
    public void Rejects_an_unknown_verdict()
    {
        var act = () => _parser.Parse("""{"verdict":"maybe","confidence":0.8,"reason":"No."}""");

        act.Should().Throw<InvalidOperationException>();
    }
}
