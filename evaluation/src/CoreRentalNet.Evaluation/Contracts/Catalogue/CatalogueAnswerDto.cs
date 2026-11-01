using System.Text.Json.Serialization;

using CoreRentalNet.Evaluation.Contracts.Suggestion;
using CoreRentalNet.Evaluation.Contracts.Reranker;
using CoreRentalNet.Evaluation.Contracts.Trace;
namespace CoreRentalNet.Evaluation.Contracts.Catalogue;

/// <summary>One catalogue answer, reduced to the SKUs the evaluator needs.</summary>
public sealed record CatalogueAnswerDto(
    [property: JsonPropertyName("matches")] CatalogueMatchesDto Matches);
