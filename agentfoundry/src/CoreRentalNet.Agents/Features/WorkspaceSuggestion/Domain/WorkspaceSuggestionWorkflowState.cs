using System.Text.Json.Serialization;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Tools;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Requests;

using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;

/// <summary>Everything one run carries between its stages, and the only lifecycle state there is.</summary>
/// <remarks>
/// <para>
/// The message that travels every edge, so it is part of every checkpoint: a resumed run knows which attempt it
/// was on and what the previous attempt produced without any of that living on an executor.
/// </para>
/// <para>
/// <b>One attempt is one cycle</b> of rephrasing, retrieval, composition, validation and review, and
/// <see cref="CompletedAttemptCount" /> is the only counter. The transport's own retry is a different mechanism
/// and never touches it.
/// </para>
/// </remarks>
public sealed class WorkspaceSuggestionWorkflowState
{
    /// <summary>The run identifier the application sent, echoed on every streamed event.</summary>
    public required string CustomerWorkflowIdentifier { get; init; }

    /// <summary>The customer's own words, verbatim, kept for every re-reading of them.</summary>
    public required string OriginalCustomerQuery { get; init; }

    /// <summary>The slots and capacities the request stated, which a composition may not exceed.</summary>
    public IReadOnlyList<SlotRule> SlotCapacityRules { get; init; } = [];

    /// <summary>Where the run is: Processing until one terminal status is reached.</summary>
    public WorkspaceSuggestionRunStatus RunStatus { get; set; } = WorkspaceSuggestionRunStatus.Processing;

    /// <summary>The stage a caller is currently being told about.</summary>
    public WorkspaceProcessingStage? CurrentProcessingStage { get; set; }

    /// <summary>How many attempts have finished. Incremented once per review.</summary>
    public int CompletedAttemptCount { get; set; }

    /// <summary>The most attempts this run may make.</summary>
    public int MaximumAttemptCount { get; init; } = 3;

    /// <summary>The verifier's verdict, set once and never revisited.</summary>
    public WorkspaceRequestVerificationResult? RequestVerification { get; set; }

    /// <summary>The currency the catalogue is priced in, which every budget in an expansion is stated in.</summary>
    /// <remarks>
    /// Carried so the rephraser is told what money it is reading about rather than inferring a currency from the
    /// customer's words, and so a budget the model states can be refused deterministically when it disagrees with
    /// the catalogue. It is the one value this agent reads out of the catalogue before a run begins.
    /// </remarks>
    public string? CatalogueCurrency { get; init; }

    /// <summary>The ceiling the application recorded, or null when the customer stated none.</summary>
    /// <remarks>
    /// The second source of a total budget, and the one that was dead before: the application has always sent it
    /// and nothing read it. The customer's own words come first and this answers for a request that named a
    /// ceiling somewhere other than the sentence.
    /// </remarks>
    public int? CustomerStatedCeilingMonthly { get; init; }

    /// <summary>The rephraser's reading of the sentence for the attempt in flight.</summary>
    public WorkspaceRequirementExpansion? RequirementExpansion { get; set; }

    /// <summary>What the catalogue search reported for the attempt in flight: availability and its searches.</summary>
    public CatalogueProductRetrievalResult? CatalogueRetrieval { get; set; }

    /// <summary>The products the searches returned for the attempt in flight, bounded per component.</summary>
    /// <remarks>
    /// Built from the recorded tool answers rather than from the retriever's report, so every value is the tool's
    /// own — the description above all, which is the text the reranker reasons over.
    /// </remarks>
    public IReadOnlyList<RetrievedWorkspaceComponentProduct> CandidatePool { get; set; } = [];

    /// <summary>The products the reranker kept, ordered, which are the only ones the composer may use.</summary>
    /// <remarks>
    /// Empty until the reranking stage has run, and never the pool: the difference between them is the difference
    /// between what a search offered and what a judge chose.
    /// </remarks>
    public IReadOnlyList<SelectedWorkspaceComponentProduct> SelectedWorkspaceCandidates { get; set; } = [];

    /// <summary>Whether the composed setups passed the deterministic structural check.</summary>
    public bool IsWorkspaceSetupStructureValid { get; set; }

    /// <summary>The setups the composer proposed for the attempt in flight.</summary>
    public IReadOnlyList<WorkspaceSetupCandidate> ProposedWorkspaceSetups { get; set; } = [];

    /// <summary>The reviewer's verdict on the attempt in flight.</summary>
    public WorkspaceSetupReviewResult? WorkspaceSetupReview { get; set; }

    /// <summary>What the retry policy decided, written and read only by deterministic code.</summary>
    public WorkspaceSetupRetryDecision? RetryDecision { get; set; }

    /// <summary>The previous attempt's review issues, and only those: the feedback is bounded on purpose.</summary>
    public IReadOnlyList<WorkspaceSetupReviewIssue> PreviousAttemptIssues { get; set; } = [];

    /// <summary>Where the retry loop has got to, and one more than the attempts finished.</summary>
    [JsonIgnore]
    public int NextAttemptNumber => CompletedAttemptCount + 1;
}
