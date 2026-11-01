using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;

namespace CoreRentalNet.Evaluation.Datasets.Model;

/// <summary>One annotated evaluation case, independent of how the agent under test produces its answer.</summary>
/// <remarks>
/// The optional annotations are what separate an evaluable case from a measurable one. A case without relevance
/// labels can still prove outcome accuracy; it cannot prove Recall@K. The validator reports that gap rather than
/// the metrics inventing a label.
/// </remarks>
public sealed record RagEvaluationCase
{
    public string Id { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string Query { get; init; } = string.Empty;

    public RagOutcome ExpectedOutcome { get; init; } = RagOutcome.Indeterminate;

    public EquipmentExpectation ExpectedEquipment { get; init; } = new(0, 0, 0, 0, 0, 0, 0);

    /// <summary>The catalogue SKUs a correct recommendation contains. A set, never a sequence.</summary>
    public IReadOnlyList<string> ExpectedWorkspaceIds { get; init; } = [];

    /// <summary>Every SKU that is relevant to the query, graded 1, for retrieval metrics.</summary>
    public IReadOnlyList<string> RelevantWorkspaceIds { get; init; } = [];

    /// <summary>Human-authored facts. When empty, <see cref="DerivedExpectedFacts"/> supplies quantity facts.</summary>
    public IReadOnlyList<string> ExpectedFacts { get; init; } = [];

    public string? ExpectedOutcomeReason { get; init; }

    public string? ParaphraseGroupId { get; init; }

    public string? NoiseVariantGroupId { get; init; }

    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>The facts a completeness or context-recall evaluator compares against, with their provenance.</summary>
    public ExpectedFacts Facts =>
        ExpectedFacts.Count > 0
            ? new ExpectedFacts(ExpectedFacts, ExpectedFactsSource.Annotated)
            : new ExpectedFacts(DerivedExpectedFacts.From(ExpectedEquipment), ExpectedFactsSource.Derived);
}
