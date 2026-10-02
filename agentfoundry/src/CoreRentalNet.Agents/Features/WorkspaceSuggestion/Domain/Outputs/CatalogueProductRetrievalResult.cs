using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
/// <summary>What the catalogue search reported: whether it could search at all, and what each search found.</summary>
/// <remarks>
/// <para>
/// <b>It no longer carries the products, and that is the point.</b> The retriever used to restate every SKU, name
/// and amount it had been given, which is a model copying catalogue data — and a copy can truncate, fuse or
/// invent without anything noticing. The products now come from the recorded tool answers, so this contract holds
/// only what a model is the right author of: whether the search worked, and what each search came back with.
/// </para>
/// <para>
/// The searches are what make an empty answer actionable: a search that found nothing <i>because a budget
/// excluded everything</i> names the cheapest product it excluded, and that figure is what a retry needs.
/// </para>
/// </remarks>
public sealed record CatalogueProductRetrievalResult(
    [property: JsonPropertyName("isAvailable")]
    bool IsAvailable,
    [property: JsonPropertyName("unavailableReason")]
    string? UnavailableReason,
    [property: JsonPropertyName("searches")]
    IReadOnlyList<WorkspaceComponentSearchOutcome> ComponentSearchOutcomes);
