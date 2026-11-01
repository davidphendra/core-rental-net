using System.Text.Json.Serialization;

using CoreRentalNet.Evaluation.Contracts.Catalogue;
using CoreRentalNet.Evaluation.Contracts.Reranker;
using CoreRentalNet.Evaluation.Contracts.Trace;
namespace CoreRentalNet.Evaluation.Contracts.Suggestion;

/// <summary>One line of an approved setup on the wire.</summary>
public sealed record CandidateLineDto(
    [property: JsonPropertyName("slot")] string Slot,
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("quantity")] int Quantity,
    [property: JsonPropertyName("amount")] decimal Amount);
