using System.Text.Json;

using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Validation;
namespace CoreRentalNet.Evaluation.Cases.Parsing;

/// <summary>The parsed script, its catalogue, and the fixture check that ran before evaluation.</summary>
public sealed record LoadedCaseSet(
    EvaluationCaseSet Cases,
    IReadOnlyDictionary<string, ProductRecord> Catalogue,
    CaseSetValidation Validation);

/// <summary>Loads the test script and catalogue from disk.</summary>
public static class CaseSetLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static LoadedCaseSet Load(string testScriptPath, string productsPath)
    {
        var products = JsonSerializer.Deserialize<IReadOnlyList<ProductRecord>>(
            File.ReadAllText(productsPath), JsonOptions)
            ?? throw new InvalidOperationException($"The catalogue at '{productsPath}' was empty.");

        var catalogue = products.ToDictionary(product => product.SkuNo, StringComparer.Ordinal);
        var parser = new TestScriptCaseParser(new ProductSkuResolver(products));
        var cases = parser.Parse(File.ReadAllText(testScriptPath));
        var validation = new CaseSetValidator().Validate(cases, catalogue);

        return new LoadedCaseSet(cases, catalogue, validation);
    }
}
