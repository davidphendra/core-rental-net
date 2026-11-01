using System.Text.Json.Serialization;

using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;
namespace CoreRentalNet.Evaluation.Cases.Model;

/// <summary>One row of the catalogue file, as the evaluator needs it.</summary>
public sealed record ProductRecord(
    [property: JsonPropertyName("skuNo")] string SkuNo,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("subCategory")] string? SubCategory,
    [property: JsonPropertyName("pricePerMonth")] decimal PricePerMonth);
