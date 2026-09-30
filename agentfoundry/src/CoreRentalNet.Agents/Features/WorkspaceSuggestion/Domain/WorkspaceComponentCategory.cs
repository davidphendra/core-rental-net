using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;

/// <summary>The seven component categories a workspace is composed from.</summary>
/// <remarks>
/// <para>
/// <b>A component category is the search's word and a composition slot is the setup's, and they are not the same
/// list.</b> A catalogue is searched by category; a setup line is written by slot. The wire words are the
/// catalogue's own, because they are arguments to a tool rather than an enum of this solution.
/// </para>
/// <para>
/// <see cref="WorkspaceComponentVocabularyMapping"/> is the only place the three vocabularies meet.
/// </para>
/// <para>
/// <b>The converter is not decoration.</b> Without it the enum serializes as its ordinal, and a category of
/// <c>0</c> is not one of the seven words the contract names — the same trap this repository has already been
/// caught by once, where a global camel-case converter overrode an enum's own wire words.
/// </para>
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<WorkspaceComponentCategory>))]
public enum WorkspaceComponentCategory
{
    [JsonStringEnumMemberName("desk")]
    Desk,

    [JsonStringEnumMemberName("chair")]
    Chair,

    [JsonStringEnumMemberName("monitor")]
    Monitor,

    [JsonStringEnumMemberName("lamp")]
    Lamp,

    [JsonStringEnumMemberName("plant")]
    Plant,

    [JsonStringEnumMemberName("bean_bag")]
    BeanBag,

    [JsonStringEnumMemberName("coffee_machine")]
    CoffeeMachine,
}
