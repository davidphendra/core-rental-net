using AwesomeAssertions;
using CoreRentalNet.Evaluation.Evaluation;
using CoreRentalNet.Evaluation.Evaluation.AgentEval;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests;

public sealed class AgentEvalAssertionCheckTests
{
    [Fact]
    public void The_observed_tool_chain_passes()
    {
        var result = AgentEvalToolChainCheck.Create()(
            ContextFixture.Item(ContextFixture.Create()));

        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void A_run_with_no_catalogue_call_fails_the_tool_chain()
    {
        var result = AgentEvalToolChainCheck.Create()(
            ContextFixture.Item(ContextFixture.Create(toolAnswers: [], rerankSelections: [])));

        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain("search_catalogue");
    }

    [Fact]
    public void The_declared_workflow_nodes_pass()
    {
        var result = AgentEvalWorkflowNodesCheck.Create()(
            ContextFixture.Item(ContextFixture.Create()));

        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void A_missing_reranker_node_fails_the_workflow_check()
    {
        var stages = new[]
        {
            "verifyingRequest",
            "rephrasingRequirement",
            "retrievingCatalogueProducts",
            "composingWorkspaceSetups",
        };

        var result = AgentEvalWorkflowNodesCheck.Create()(
            ContextFixture.Item(ContextFixture.Create(stages: stages)));

        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain("rerankingWorkspaceCandidates");
    }

    [Fact]
    public void A_non_success_run_fails_the_workflow_check()
    {
        var result = AgentEvalWorkflowNodesCheck.Create()(
            ContextFixture.Item(ContextFixture.Create(runStatus: "unavailable")));

        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain("unavailable");
    }
}
