using AwesomeAssertions;
using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests.Execution;

public sealed class EvaluationScopeParserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void An_absent_scope_is_tiny(string? value)
    {
        EvaluationScopeParser.Parse(value).Should().Be(EvaluationScope.Tiny);
    }

    [Theory]
    [InlineData("tiny", EvaluationScope.Tiny)]
    [InlineData("SMALL", EvaluationScope.Small)]
    [InlineData("Half", EvaluationScope.Half)]
    [InlineData("all", EvaluationScope.All)]
    public void A_scope_name_is_read_without_regard_to_case(string value, EvaluationScope expected)
    {
        EvaluationScopeParser.Parse(value).Should().Be(expected);
    }

    [Theory]
    [InlineData("medium")]
    [InlineData("2")]
    public void An_unknown_scope_is_refused_by_name(string value)
    {
        var act = () => EvaluationScopeParser.Parse(value);

        act.Should().Throw<ArgumentException>().WithMessage($"*{value}*");
    }
}
