using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace WorkspaceSuggestions.Workflows;

/// <summary>The pipeline this deployable serves, published as the one agent the host registers.</summary>
/// <remarks>
/// A port rather than a class, so the composition root names the pipeline without naming the shape it has
/// today. The tools are a parameter because they belong to the call, not to the pipeline.
/// </remarks>
internal interface IWorkspaceWorkflow
{
    /// <summary>The pipeline as one agent, built with the catalogue tools this call may use.</summary>
    AIAgent AsAIAgent(IReadOnlyList<AITool> tools);
}
