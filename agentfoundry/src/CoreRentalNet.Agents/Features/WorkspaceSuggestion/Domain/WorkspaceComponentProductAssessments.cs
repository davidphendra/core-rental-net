using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;

/// <summary>The reranker's judgement on each component's products, one list per component, all seven present.</summary>
/// <remarks>
/// <para>
/// <b>Seven properties rather than one ranked list, because coverage is what a single list cannot promise.</b>
/// Ranked together, fifteen strong desks push the seating out of the answer entirely — and an omitted component
/// is not a ranking mistake but a workspace that cannot be composed. An empty list is a judgement ("none of these
/// answer it"); an absent component is not.
/// </para>
/// <para>
/// <b>Declared separately from <see cref="WorkspaceComponentExpansions"/> on purpose.</b> The two look alike and
/// are two contracts with two owners — one is the rephraser's output, the other the reranker's — so a shared
/// container would couple them, and a change to one could silently change the other's wire shape. The repetition
/// of seven names is the price of their independence, and every property being required means the deserializer
/// refuses an answer that omits one.
/// </para>
/// </remarks>
public sealed record WorkspaceComponentProductAssessments(
    [property: JsonPropertyName("desk")]
    [property: JsonRequired]
    IReadOnlyList<WorkspaceComponentProductAssessment> Desk,
    [property: JsonPropertyName("chair")]
    [property: JsonRequired]
    IReadOnlyList<WorkspaceComponentProductAssessment> Chair,
    [property: JsonPropertyName("monitor")]
    [property: JsonRequired]
    IReadOnlyList<WorkspaceComponentProductAssessment> Monitor,
    [property: JsonPropertyName("lamp")]
    [property: JsonRequired]
    IReadOnlyList<WorkspaceComponentProductAssessment> Lamp,
    [property: JsonPropertyName("plant")]
    [property: JsonRequired]
    IReadOnlyList<WorkspaceComponentProductAssessment> Plant,
    [property: JsonPropertyName("bean_bag")]
    [property: JsonRequired]
    IReadOnlyList<WorkspaceComponentProductAssessment> BeanBag,
    [property: JsonPropertyName("coffee_machine")]
    [property: JsonRequired]
    IReadOnlyList<WorkspaceComponentProductAssessment> CoffeeMachine)
{
    /// <summary>Every component with its assessments, in the workspace's own order.</summary>
    public IEnumerable<(
        WorkspaceComponentCategory ComponentCategory,
        IReadOnlyList<WorkspaceComponentProductAssessment> Assessments)> EveryComponent()
    {
        yield return (WorkspaceComponentCategory.Desk, Desk);
        yield return (WorkspaceComponentCategory.Chair, Chair);
        yield return (WorkspaceComponentCategory.Monitor, Monitor);
        yield return (WorkspaceComponentCategory.Lamp, Lamp);
        yield return (WorkspaceComponentCategory.Plant, Plant);
        yield return (WorkspaceComponentCategory.BeanBag, BeanBag);
        yield return (WorkspaceComponentCategory.CoffeeMachine, CoffeeMachine);
    }
}
