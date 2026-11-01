using System.Text.Json;
using CoreRentalNet.Evaluation;

using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Validation;
namespace CoreRentalNet.Evaluation.Datasets.Loading;

/// <summary>Deserialises the annotated JSON datasets without repairing them.</summary>
/// <remarks>
/// A malformed file throws. Quiet coercion would let a typo in a lockbox case become a silent pass.
/// </remarks>
internal static class JsonCaseReader
{
    public static IReadOnlyList<RagEvaluationCase> Read(byte[] utf8Json)
    {
        var cases = JsonSerializer.Deserialize<List<RagEvaluationCase>>(utf8Json, EvaluationJson.Options);

        return cases ?? throw new InvalidOperationException("The dataset file contained no JSON array.");
    }
}
