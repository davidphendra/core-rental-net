namespace CoreRentalNet.Evaluation.Evidence;

/// <summary>The stage identities the evaluator pins against the agent's roster.</summary>
public static class WorkspaceStageNames
{
    public const string Verifier = "workspace-request-verifier";
    public const string Rephraser = "workspace-requirement-rephraser";
    public const string Retriever = "catalogue-product-retriever";
    public const string Reranker = "catalogue-candidate-reranker";
    public const string Composer = "workspace-setup-composer";
    public const string Reviewer = "workspace-setup-reviewer";
}
