using AwesomeAssertions;
using CoreRentalNet.Evaluation.Configuration;
using CoreRentalNet.Evaluation.Evaluation;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests;

public sealed class EvaluationJudgeFactoryTests
{
    [Fact]
    public void No_endpoint_means_no_judge()
        => EvaluationJudgeFactory.Create(new EvaluationOptions()).Should().BeNull();

    [Fact]
    public void A_local_endpoint_creates_the_local_judge()
    {
        var options = new EvaluationOptions
        {
            JudgeEndpoint = new Uri("http://localhost:8080/v1"),
        };

        EvaluationJudgeFactory.Create(options).Should().BeOfType<LocalJudgeEvaluator>();
    }
}
