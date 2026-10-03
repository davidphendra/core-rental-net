using Microsoft.Extensions.AI;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
using CoreRentalNet.Agents.Shared.Agents;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Agents;

/// <summary>The stage agents this deployable serves, as data.</summary>
/// <remarks>
/// <para>
/// The one place a stage's <b>name</b> and its prompt file are written down together. The name is also the
/// identity a workflow checkpoint records, so renaming one here is a resume-compatibility decision rather than a
/// rename.
/// </para>
/// <para>
/// One stage, one decision. The verifier decides whether the sentence is a workspace request; the rephraser
/// decides what it asks for and what to look for; the retriever decides what the catalogue offers; the composer
/// decides which products form a setup; the reviewer decides whether the setup set satisfies the request.
/// Nothing decides its own next step: that is the workflow's and the retry policy's.
/// </para>
/// </remarks>
internal static class WorkspaceSuggestionAgentRoster
{
    public static AgentProfile Verifier { get; } = new(
        Name: "workspace-request-verifier",
        PromptFileName: "workspace-request-verification.v1.md",
        Description: "Decides whether the customer's sentence is a request to furnish a workspace at all.",
        Output: ChatResponseFormat.ForJsonSchema<WorkspaceRequestVerificationResult>());

    public static AgentProfile Rephraser { get; } = new(
        Name: "workspace-requirement-rephraser",
        PromptFileName: "workspace-retrieval-requirement.v1.md",
        Description: "Turns the sentence into what the workspace must be and the words that find each part of it.",
        Output: ChatResponseFormat.ForJsonSchema<WorkspaceRequirementExpansion>());

    public static AgentProfile Retriever { get; } = new(
        Name: "catalogue-product-retriever",
        PromptFileName: "catalogue-product-retrieval.v2.md",
        Description: "Finds the catalogue products that could satisfy the expansion, and composes nothing.",
        Output: ChatResponseFormat.ForJsonSchema<CatalogueProductRetrievalResult>(),
        UsesCatalogueTools: true);

    public static AgentProfile Reranker { get; } = new(
        Name: "catalogue-candidate-reranker",
        PromptFileName: "workspace-candidate-reranking.v1.md",
        Description: "Orders each component's retrieved products against the need that component must answer.",
        Output: ChatResponseFormat.ForJsonSchema<WorkspaceComponentProductRankingResult>());

    public static AgentProfile Composer { get; } = new(
        Name: "workspace-setup-composer",
        PromptFileName: "workspace-setup-composition.v1.md",
        Description: "Composes candidate setups from the retrieved products and the expansion.",
        Output: ChatResponseFormat.ForJsonSchema<WorkspaceSetupCandidateSet>());

    public static AgentProfile Reviewer { get; } = new(
        Name: "workspace-setup-reviewer",
        PromptFileName: "workspace-setup-review.v1.md",
        Description: "Decides whether the composed setups satisfy the customer's request.",
        Output: ChatResponseFormat.ForJsonSchema<WorkspaceSetupReviewResult>());

    /// <summary>In the order the workflow runs them, for the transcript and the roster test.</summary>
    public static IReadOnlyList<AgentProfile> All { get; } =
        [Verifier, Rephraser, Retriever, Reranker, Composer, Reviewer];

    /// <summary>The prompts a run used, named so a record can say which instructions produced it.</summary>
    public static string PromptVersions { get; } = string.Join(
        ", ",
        All.Select(profile => Path.GetFileNameWithoutExtension(profile.PromptFileName)));
}
