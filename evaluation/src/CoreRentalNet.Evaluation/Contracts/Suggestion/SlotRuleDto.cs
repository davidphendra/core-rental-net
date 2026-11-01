using System.Text.Json.Serialization;

using CoreRentalNet.Evaluation.Contracts.Catalogue;
using CoreRentalNet.Evaluation.Contracts.Reranker;
using CoreRentalNet.Evaluation.Contracts.Trace;
namespace CoreRentalNet.Evaluation.Contracts.Suggestion;

/// <summary>One slot rule on the wire.</summary>
public sealed record SlotRuleDto(
    [property: JsonPropertyName("slot")] string Slot,
    [property: JsonPropertyName("capacity")] int Capacity);
