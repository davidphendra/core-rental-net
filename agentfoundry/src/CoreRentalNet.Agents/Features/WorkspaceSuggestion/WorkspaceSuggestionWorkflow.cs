using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion;

/// <summary>The workspace agent factory the host resolves once per request.</summary>
/// <remarks>
/// A port rather than the graph itself, so the composition root names the pipeline without naming its shape. The
/// graph is built per resolution by <see cref="WorkspaceSuggestionWorkflowFactory"/>, because the call's catalogue
/// tools belong to the call and not to the process, and because a workflow has one owner at a time. The factory
/// itself is stateless and remains a singleton.
/// </remarks>
internal sealed class WorkspaceSuggestionWorkflow(
    WorkspaceSuggestionExecutorBuilder workspaceSuggestionExecutorBuilder,
    WorkspaceSuggestionAgentIdentity workspaceSuggestionAgentIdentity) : IWorkspaceSuggestionWorkflow
{
    /// <summary>The workflow as one agent, its yielded events made visible to the caller.</summary>
    /// <remarks>
    /// <b>The served agent is the workflow agent itself and is never wrapped.</b> Hosting can redirect a hosted
    /// workflow's checkpoints only when it can copy that agent, and a wrapper cannot be copied. Every call creates
    /// a new workflow, so two concurrent requests never share a workflow owner. The per-call token and catalogue
    /// tools are applied one layer down, at each stage's model call.
    /// </remarks>
    public AIAgent AsAIAgent()
        => WorkspaceSuggestionWorkflowFactory.Create(workspaceSuggestionExecutorBuilder)
            .AsAIAgent(
                name: workspaceSuggestionAgentIdentity.AgentName,

                // This is what turns each yielded event into one response delta. Without it a workflow event
                // reaches the caller as an empty update and the stream carries nothing at all.
                includeWorkflowOutputsInResponse: true
            );
}
