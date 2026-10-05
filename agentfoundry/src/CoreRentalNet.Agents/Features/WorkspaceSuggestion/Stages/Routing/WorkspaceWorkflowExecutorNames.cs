using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Agents;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Routing;

/// <summary>The executor names the graph is wired by, in one place.</summary>
/// <remarks>
/// <para>
/// A name is also what a checkpoint records, so these are stable strings rather than display labels: changing
/// one is a resume-compatibility decision, and the graph and the routes read them from here so they cannot
/// drift apart.
/// </para>
/// <para>
/// The stage nodes' names are the roster's own. The roster is where a stage's name and its prompt file are
/// written down together, so the identity a checkpoint records is the identity the prompt report already names.
/// The nodes with no agent and no prompt — the input, the pool, the validator, the retry decision and the three
/// completions — keep their literal names here.
/// </para>
/// </remarks>
internal static class WorkspaceWorkflowExecutorNames
{
    public const string Input = "read-workspace-suggestion-request";

    public static readonly string Verifier = WorkspaceSuggestionAgentRoster.Verifier.Name;
    public static readonly string Rephraser = WorkspaceSuggestionAgentRoster.Rephraser.Name;
    public static readonly string Retriever = WorkspaceSuggestionAgentRoster.Retriever.Name;
    public static readonly string Reranker = WorkspaceSuggestionAgentRoster.Reranker.Name;

    public const string ProductPool = "build-candidate-product-pool";

    public static readonly string Composer = WorkspaceSuggestionAgentRoster.Composer.Name;
    public static readonly string Reviewer = WorkspaceSuggestionAgentRoster.Reviewer.Name;

    public const string Validator = "validate-workspace-setup-structure";
    public const string RetryDecision = "decide-workspace-setup-retry";
    public const string SuccessCompletion = "complete-workspace-suggestion-success";
    public const string RejectionCompletion = "complete-workspace-suggestion-rejection";
    public const string UnavailableCompletion = "complete-workspace-suggestion-unavailable";
}
