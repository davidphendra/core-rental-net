using AwesomeAssertions;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Agents;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Routing;
using CoreRentalNet.Agents.Shared.Mcp;
using CoreRentalNet.Agents.Shared.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The node list the graph is wired from: every node, once, under the name a checkpoint records.</summary>
/// <remarks>
/// The graph's outputs are declared from this list, so the rules worth pinning are the name each node is wired by
/// and that no node is published twice or left out.
/// </remarks>
public sealed class WorkspaceSuggestionExecutorsTests
{
    [Fact]
    public void Every_node_is_published_under_the_name_the_graph_wires_it_by()
    {
        var executors = Build();

        executors.All.Select(binding => binding.Id).Should().Equal(
            WorkspaceWorkflowExecutorNames.Input,
            WorkspaceWorkflowExecutorNames.Verifier,
            WorkspaceWorkflowExecutorNames.Rephraser,
            WorkspaceWorkflowExecutorNames.Retriever,
            WorkspaceWorkflowExecutorNames.ProductPool,
            WorkspaceWorkflowExecutorNames.Reranker,
            WorkspaceWorkflowExecutorNames.Composer,
            WorkspaceWorkflowExecutorNames.Validator,
            WorkspaceWorkflowExecutorNames.Reviewer,
            WorkspaceWorkflowExecutorNames.RetryDecision,
            WorkspaceWorkflowExecutorNames.SuccessCompletion,
            WorkspaceWorkflowExecutorNames.RejectionCompletion,
            WorkspaceWorkflowExecutorNames.UnavailableCompletion);
    }

    [Fact] // the outputs are this list, so a duplicate would publish a node twice and a missing one never
    public void The_published_nodes_are_every_node_and_none_twice()
    {
        var executors = Build();

        executors.All.Should().HaveCount(13);
        executors.All.Select(binding => binding.Id).Should().OnlyHaveUniqueItems();
    }

    private static WorkspaceSuggestionExecutors Build()
    {
        var tokens = new McpAccessTokenService(NullLogger<McpAccessTokenService>.Instance);
        var recordedToolAnswers = new McpToolAnswerLedger();
        var runUsage = new AgentRunUsageAccumulator("gpt-4.1-mini", "test-prompts");

        var stageAgents = new WorkspaceSuggestionAgentBuilder(
            new ScriptedChatClient("{}"),
            tokens,
            new McpAuthorizationConnection(
                new McpSetting(string.Empty),
                tokens,
                NullLoggerFactory.Instance,
                NullLogger<McpAuthorizationConnection>.Instance),
            recordedToolAnswers,
            runUsage,
            NullLoggerFactory.Instance);

        return new WorkspaceSuggestionExecutorBuilder(
            stageAgents,
            tokens,
            TheInvocationARunArrivesIn.CarryingNothing(),
            new WorkspaceSuggestionWorkflowOptions(),
            recordedToolAnswers,
            runUsage).Build();
    }
}
