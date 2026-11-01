using AwesomeAssertions;
using CoreRentalNet.Evaluation.Evaluators.Judges;
using CoreRentalNet.Evaluation.Evaluators.Correctness;
using CoreRentalNet.Evaluation.Evaluators.Shared;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests.Integration;

/// <summary>Live checks that require the local stack. They are skipped, not silently green.</summary>
/// <remarks>
/// A skip with a reason is a statement that the path is unproven. Turning these into passing tests without the
/// stack would be the one thing this harness must never do.
/// </remarks>
public sealed class LiveIntegrationTests
{
    private const string Reason = "Requires the local stack: agent 127.0.0.1:8088, judge 127.0.0.1:8080.";

    [Fact(Skip = Reason)]
    public void The_agent_answers_one_case_end_to_end()
        => Assert.Fail("Run manually against the local stack.");

    [Fact(Skip = Reason)]
    public async Task The_judge_scores_a_rendered_recommendation()
    {
        var judge = CoreRentalNet.Evaluation.Evaluation.Judging.OpenAiCompatibleJudgeClientFactory.Create(
            new Uri("http://127.0.0.1:8080"), "local", "gpt-oss-20b");

        var score = await new RelevanceJudge(judge).EvaluateAsync(
            JudgeInput.From(new CoreRentalNet.Evaluation.Runs.Result.RagRunResult { CaseId = "adhoc-1", Query = "desk and chair", AnswerText = "Outcome: success." }));

        score.Evaluable.Should().BeTrue();
    }
}
