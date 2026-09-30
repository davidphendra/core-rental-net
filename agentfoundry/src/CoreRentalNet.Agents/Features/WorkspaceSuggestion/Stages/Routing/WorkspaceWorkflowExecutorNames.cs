namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Routing;

/// <summary>The executor names the graph is wired by, in one place.</summary>
/// <remarks>
/// A name is also what a checkpoint records, so these are stable strings rather than display labels: changing
/// one is a resume-compatibility decision, and the graph and the routes read them from here so they cannot
/// drift apart.
/// </remarks>
internal static class WorkspaceWorkflowExecutorNames
{
    public const string Input = "read-workspace-suggestion-request";
    public const string Verifier = "verify-workspace-request";
    public const string Rephraser = "rephrase-workspace-requirement";
    public const string Retriever = "retrieve-catalogue-products";
    public const string ProductPool = "build-candidate-product-pool";
    public const string Reranker = "rerank-workspace-candidates";

    public const string Composer = "compose-workspace-setups";
    public const string Validator = "validate-workspace-setup-structure";
    public const string Reviewer = "review-workspace-setups";
    public const string RetryDecision = "decide-workspace-setup-retry";
    public const string SuccessCompletion = "complete-workspace-suggestion-success";
    public const string RejectionCompletion = "complete-workspace-suggestion-rejection";
    public const string UnavailableCompletion = "complete-workspace-suggestion-unavailable";
}
