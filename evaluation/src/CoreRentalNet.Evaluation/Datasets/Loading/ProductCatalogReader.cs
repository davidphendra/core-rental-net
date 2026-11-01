using System.Text.Json;
using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;

using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Validation;
namespace CoreRentalNet.Evaluation.Datasets.Loading;

/// <summary>Reads the catalogue beside the dataset, because the development script names products, not slots.</summary>
internal static class ProductCatalogReader
{
    private static readonly string[] CandidateNames =
    [
        Path.Combine("src", "shared", "data", "products.json"),
        "products.json",
    ];

    public static IReadOnlyList<ProductRecord> Read(string? directory)
    {
        var root = directory ?? Directory.GetCurrentDirectory();
        var path = CandidateNames
            .Select(name => Path.Combine(root, name))
            .FirstOrDefault(File.Exists)
            ?? Path.Combine(root, CandidateNames[0]);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"The catalogue beside the development dataset was not found at '{path}'.",
                path);
        }

        var products = JsonSerializer.Deserialize<List<ProductRecord>>(
            File.ReadAllText(path),
            EvaluationJson.Options);

        return products ?? throw new InvalidOperationException($"The catalogue at '{path}' was empty.");
    }
}
