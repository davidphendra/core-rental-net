using Microsoft.Agents.AI;

namespace WorkspaceSuggestions.Workflows;

/// <summary>The pipeline this deployable serves, published as the single agent the host resolves.</summary>
/// <remarks>
/// A port rather than a class, because the host must be able to publish the pipeline without knowing whether
/// it is a straight line today or a graph with branches tomorrow.
/// </remarks>
internal interface IWorkspaceWorkflow
{
    /// <summary>The pipeline, as the one agent Foundry serves.</summary>
    AIAgent AsAIAgent();
}
