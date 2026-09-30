using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;

/// <summary>The rephraser's whole reading: what the workspace is for, what it may cost, and the words that find
/// each part of it.</summary>
/// <remarks>
/// <para>
/// No SKU, no product name and no product price: the expansion decides what is wanted and how to look for it, and
/// the composition decides which product supplies it. That separation is what makes every retrieved product
/// traceable to a search rather than to a memory.
/// </para>
/// <para>
/// <b>It replaced a specification that carried only slots and a ceiling.</b> What the words are for is the
/// difference: the old reading could say what was wanted and not how to find it, which is why a described need
/// reached a name search as one long phrase and matched nothing.
/// </para>
/// </remarks>
public sealed record WorkspaceRequirementExpansion(
    [property: JsonPropertyName("original_query")]
    [property: JsonRequired]
    string OriginalCustomerQuery,
    [property: JsonPropertyName("workspace_intent")]
    [property: JsonRequired]
    WorkspaceIntent WorkspaceIntent,
    [property: JsonPropertyName("total_budget")]
    [property: JsonRequired]
    WorkspaceTotalMonthlyBudget TotalMonthlyBudget,
    [property: JsonPropertyName("categories")]
    [property: JsonRequired]
    WorkspaceComponentExpansions ComponentExpansions);
