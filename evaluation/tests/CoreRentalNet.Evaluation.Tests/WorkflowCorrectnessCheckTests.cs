using AwesomeAssertions;
using CoreRentalNet.Evaluation.Evaluation;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests;

public sealed class WorkflowCorrectnessCheckTests
{
    [Fact]
    public void The_successful_sequence_passes()
    {
        WorkflowCorrectnessCheck.Create()(ContextFixture.Item(ContextFixture.Create()))
            .Passed.Should().BeTrue();
    }

    [Fact]
    public void A_missing_reviewer_fails()
    {
        var stages = ContextFixture.SuccessStages().Take(6).ToArray();

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
            "validatingWorkspaceSetups",
            "rephrasingRequirement",
            "retrievingCatalogueProducts",
            "rerankingWorkspaceCandidates",
            "composingWorkspaceSetups",
            "validatingWorkspaceSetups",
            "reviewingWorkspaceSetups",
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
