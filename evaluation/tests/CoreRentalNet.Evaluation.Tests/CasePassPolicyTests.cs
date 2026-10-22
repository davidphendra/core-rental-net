using AwesomeAssertions;
using CoreRentalNet.Evaluation.Evaluation;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests;

/// <summary>The majority rule must not make a single-repetition run impossible to pass.</summary>
public sealed class CasePassPolicyTests
{
    [Fact]
    public void One_repetition_that_passes_is_a_passing_case()
        => CasePassPolicy.Passed(passCount: 1, repetitionCount: 1).Should().BeTrue();

    [Fact]
    public void One_repetition_that_fails_is_a_failing_case()
        => CasePassPolicy.Passed(passCount: 0, repetitionCount: 1).Should().BeFalse();

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public void Three_repetitions_need_a_majority(int passCount, bool expected)
        => CasePassPolicy.Passed(passCount, repetitionCount: 3).Should().Be(expected);

    [Fact]
    public void No_repetitions_is_not_a_passing_case()
        => CasePassPolicy.Passed(passCount: 0, repetitionCount: 0).Should().BeFalse();
}
