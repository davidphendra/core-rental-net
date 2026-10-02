using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
/// <summary>All seven component categories, always present, in the order a workspace is read.</summary>
/// <remarks>
/// <para>
/// <b>Seven properties rather than a dictionary.</b> A dictionary generates an object with no required keys, so a
/// model that returned five would be accepted and the five searched as though they were all; and a missing
/// <c>relevant</c> would default to false and silently drop a component the customer asked for. Seven
/// <see cref="JsonRequiredAttribute"/> properties turn each absence into a refusal where the answer is read.
/// </para>
/// <para>
/// <see cref="Every"/> is the single place the seven are enumerated, so no stage can walk a different set from
/// the one that was read.
/// </para>
/// </remarks>
public sealed record WorkspaceComponentExpansions(
    [property: JsonPropertyName("desk")]
    [property: JsonRequired]
    WorkspaceComponentExpansion Desk,
    [property: JsonPropertyName("chair")]
    [property: JsonRequired]
    WorkspaceComponentExpansion Chair,
    [property: JsonPropertyName("monitor")]
    [property: JsonRequired]
    WorkspaceComponentExpansion Monitor,
    [property: JsonPropertyName("lamp")]
    [property: JsonRequired]
    WorkspaceComponentExpansion Lamp,
    [property: JsonPropertyName("plant")]
    [property: JsonRequired]
    WorkspaceComponentExpansion Plant,
    [property: JsonPropertyName("bean_bag")]
    [property: JsonRequired]
    WorkspaceComponentExpansion BeanBag,
    [property: JsonPropertyName("coffee_machine")]
    [property: JsonRequired]
    WorkspaceComponentExpansion CoffeeMachine)
{
    /// <summary>Every component category with its expansion, in the workspace's own order.</summary>
    public IEnumerable<(WorkspaceComponentCategory ComponentCategory, WorkspaceComponentExpansion Expansion)> Every()
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
