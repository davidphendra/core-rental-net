using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using WorkspaceSuggestions.Agents;

namespace WorkspaceSuggestions.Workflows;

/// <summary>Rephraser, then suggestor, sequentially — and nothing else.</summary>
/// <remarks>
/// <para>
/// Built with <see cref="AgentWorkflowBuilder.BuildSequential"/> rather than a hand-wired
/// <see cref="WorkflowBuilder"/> because a straight line has no edges worth naming. The moment the pipeline
/// branches — a repair loop, a fallback, a human-in-the-loop gate — this becomes a <c>WorkflowBuilder</c>
/// with explicit edges, and callers of <see cref="IWorkspaceWorkflow"/> do not change.
/// </para>
/// <para>
/// <b>Default chaining is required.</b> <c>chainOnlyAgentResponses: true</c> would pass the suggestor only
/// the rephraser's specification and take the catalogue away with it, because the catalogue arrived in the
/// original request rather than in the spec. That is the difference between composing from the catalogue and
/// composing from memory.
/// </para>
/// </remarks>
internal sealed class WorkspaceSuggestionWorkflow(IChatClient chatClient) : IWorkspaceWorkflow
{
    /// <summary>The name Foundry resolves, and the identity the run record names.</summary>
    public const string AgentName = "core-rental-workspace-suggestion-agent";

    public AIAgent AsAIAgent(IReadOnlyList<AITool> tools)
        => AgentWorkflowBuilder
            .BuildSequential(
                AgentFactory.Build(AgentRoster.Rephraser, chatClient),
                AgentFactory.Build(AgentRoster.Suggestor, chatClient, tools))
            .AsAIAgent(name: AgentName);
}
