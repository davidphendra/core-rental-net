using AwesomeAssertions;
using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;
using Xunit;

using CoreRentalNet.Evaluation.Tests.Fixtures;

namespace CoreRentalNet.Evaluation.Tests.Checks;

public sealed class WorkflowCorrectnessCheckTests
{
    [Fact]
    public void The_successful_sequence_passes()
    {
        WorkflowCorrectnessCheck.Create()(ContextFixture.Item(ContextFixture.Create()))
            .Passed.Should().BeTrue();
    }

    [Fact]
    public void A_missing_composer_fails()
    {
        var stages = ContextFixture.SuccessStages().Take(3).ToArray();

        var result = WorkflowCorrectnessCheck.Create()(
            ContextFixture.Item(ContextFixture.Create(stages: stages)));

        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain(FailureCodes.WorkflowSequence);
    }

    [Fact]
    public void A_retry_that_returns_to_rephrasing_and_then_succeeds_passes()
    {
        var stages = new[]
        {
            "verifyingRequest",
            "rephrasingRequirement",
            "retrievingCatalogueProducts",
            "rerankingWorkspaceCandidates",
            "composingWorkspaceSetups",
            "rephrasingRequirement",
            "retrievingCatalogueProducts",
            "rerankingWorkspaceCandidates",
            "composingWorkspaceSetups",
        };

        WorkflowCorrectnessCheck.Create()(
            ContextFixture.Item(ContextFixture.Create(stages: stages, finalAttempt: 2)))
            .Passed.Should().BeTrue();
    }

    [Fact]
    public void A_non_success_status_fails()
    {
        var result = WorkflowCorrectnessCheck.Create()(
            ContextFixture.Item(ContextFixture.Create(runStatus: "unavailable")));

        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain("WORKFLOW_STATUS");
    }
}
