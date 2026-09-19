using System.ComponentModel;
using System.Text.Json.Serialization;

namespace WorkspaceSuggestions.Contracts;

/// <summary>The rephraser's output and the suggestor's input: what the customer wants, before any product is chosen.</summary>
/// <remarks>
/// The wire shape of <c>workspace-spec.schema.json</c>. A typed envelope with a natural-language purpose per
/// slot — machine-checkable structure, human-meaningful content. It carries <b>no SKU, no price, no product
/// name and no band label</b>; a specification that named a product would collapse the separation between
/// deciding what is wanted and choosing which product supplies it.
/// </remarks>
public sealed record WorkspaceSpec(
    [property: JsonPropertyName("status")]
    [property: Description("Spec when the sentence was about a workspace, NotWorkspace when it was not.")]
    SpecificationStatus Status,
    [property: JsonPropertyName("reason")]
    [property: Description("A one-line reason, present when the status is notWorkspace.")]
    string? Reason,
    [property: JsonPropertyName("ceilingMonthly")]
    [property: Description("The ceiling the customer named, or null when they named none. Never invented.")]
    int? CeilingMonthly,
    [property: JsonPropertyName("slots")]
    [property: Description("The slots the customer cares about. A slot the sentence said nothing about is left out.")]
    IReadOnlyList<SpecifiedSlot> Slots,
    [property: JsonPropertyName("constraints")]
    [property: Description("The customer's constraints in their own words - 'a small room', 'must be quiet'.")]
    IReadOnlyList<string> Constraints);
