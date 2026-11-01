using AwesomeAssertions;
using CoreRentalNet.Evaluation.Evaluators.Judges;
using CoreRentalNet.Evaluation.Evaluators.Correctness;
using CoreRentalNet.Evaluation.Evaluators.Shared;
using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests.Judges;

/// <summary>The judge reads the request and the rendered recommendation, and nothing else.</summary>
public sealed class JudgeInputTests
{
    [Fact]
    public void The_input_carries_the_query_and_the_rendered_answer()
    {
        var input = JudgeInput.From(new RagRunResult
        {
            CaseId = "TST-001",
            Query = "one desk and one chair",
            AnswerText = "Outcome: success. Recommended workspace: Desk: DSKB08XN4JDR x1.",
        });

        input.Messages.Single().Text.Should().Be("one desk and one chair");
        input.Response.Text.Should().Contain("DSKB08XN4JDR").And.StartWith("Outcome: success");
    }

    [Fact]
    public void Retrieved_products_are_not_part_of_the_judge_input()
    {
        var input = JudgeInput.From(new RagRunResult
        {
            CaseId = "TST-001",
            Query = "q",
            AnswerText = "answer",
            RetrievedSkus = ["MONB086Q776F"],
        });

        string.Join(" ", input.Messages.Select(message => message.Text)).Should().NotContain("MONB086Q776F");
    }
}
