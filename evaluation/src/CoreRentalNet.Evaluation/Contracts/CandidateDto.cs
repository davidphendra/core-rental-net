using System.Text.Json.Serialization;

namespace CoreRentalNet.Evaluation.Contracts;

/// <summary>One approved workspace setup on the wire.</summary>
public sealed record CandidateDto(
    [property: JsonPropertyName("lines")] IReadOnlyList<CandidateLineDto> Lines);
