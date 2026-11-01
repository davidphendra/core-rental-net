using System.Text.Json.Serialization;

using CoreRentalNet.Evaluation.Contracts.Catalogue;
using CoreRentalNet.Evaluation.Contracts.Reranker;
using CoreRentalNet.Evaluation.Contracts.Trace;
namespace CoreRentalNet.Evaluation.Contracts.Suggestion;

/// <summary>One approved workspace setup on the wire.</summary>
public sealed record CandidateDto(
    [property: JsonPropertyName("lines")] IReadOnlyList<CandidateLineDto> Lines);
