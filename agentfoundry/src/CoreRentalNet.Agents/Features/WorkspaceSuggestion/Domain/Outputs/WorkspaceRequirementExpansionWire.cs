using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;

/// <summary>The rephraser's compact answer: only relevant categories, a total, and optional allocations.</summary>
/// <remarks>
/// <para>
/// This is the contract the model sees. The domain type keeps all seven categories so the rest of the pipeline
/// can walk one fixed vocabulary; the mapper turns a missing category into an irrelevant one.
/// </para>
/// <para>
/// The customer sentence is deliberately absent. The workflow already holds it, and asking a model to echo it
/// spends output tokens on a value the application can copy more reliably.
/// </para>
/// </remarks>
internal sealed record WorkspaceRequirementExpansionWire(
    [property: JsonPropertyName("workspace_intent")]
    [property: JsonRequired]
    WorkspaceIntentWire WorkspaceIntent,
    [property: JsonPropertyName("total_budget")]
    [property: JsonRequired]
    WorkspaceTotalMonthlyBudget TotalMonthlyBudget,
    [property: JsonPropertyName("categories")]
    [property: JsonRequired]
    IReadOnlyList<WorkspaceComponentExpansionWire> ComponentExpansions,
    [property: JsonPropertyName("allocations")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<WorkspaceComponentAllocationWire>? Allocations = null);
