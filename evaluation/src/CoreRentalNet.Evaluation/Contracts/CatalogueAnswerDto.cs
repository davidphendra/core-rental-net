using System.Text.Json.Serialization;

namespace CoreRentalNet.Evaluation.Contracts;

/// <summary>One catalogue answer, reduced to the SKUs the evaluator needs.</summary>
public sealed record CatalogueAnswerDto(
    [property: JsonPropertyName("matches")] CatalogueMatchesDto Matches);
