using System.Text;
using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;

using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Validation;
namespace CoreRentalNet.Evaluation.Datasets.Loading;

/// <summary>Adapts the markdown development script into the annotated case model.</summary>
/// <remarks>
/// The script predates the annotation schema: it has no categories and no outcomes, and forty-three rows seat the
/// user with a bean bag where the current rules demand exactly one chair. Those rows are excluded with their
/// reason rather than relabelled, because calling them successes would score a correct refusal as a failure and
/// calling them rejections would contradict the script they came from.
/// </remarks>
internal static class DevelopmentCaseReader
{
    public static (IReadOnlyList<RagEvaluationCase> Cases, IReadOnlyList<DatasetExclusion> Exclusions) Read(
        byte[] utf8Markdown,
        string? directory)
    {
        var markdown = Encoding.UTF8.GetString(utf8Markdown);
        var products = ProductCatalogReader.Read(directory);
        var caseSet = new TestScriptCaseParser(new ProductSkuResolver(products)).Parse(markdown);

        var cases = new List<RagEvaluationCase>(caseSet.Cases.Count);
        var exclusions = new List<DatasetExclusion>();

        foreach (var testCase in caseSet.Cases)
        {
            var equipment = SlotQuantities.From(testCase.Variants[0], products);

            if (!equipment.SatisfiesMandatoryConstraints)
            {
                exclusions.Add(new DatasetExclusion(
                    testCase.CaseId,
                    "The script seats the user with a bean bag where the rules require exactly one chair."));
                continue;
            }

            cases.Add(ToCase(testCase, equipment));
        }

        return (cases, exclusions);
    }

    private static RagEvaluationCase ToCase(EvaluationCase testCase, EquipmentExpectation equipment)
    {
        var variant = testCase.Variants[0];

        return new RagEvaluationCase
        {
            Id = testCase.CaseId,
            Category = "uncategorized",
            Query = testCase.Sentence,
            ExpectedOutcome = RagOutcome.Success,
            ExpectedEquipment = equipment,
            ExpectedWorkspaceIds = [.. variant.Skus],
            RelevantWorkspaceIds = [.. variant.Skus],
            ExpectedOutcomeReason = "The script states a setup that satisfies every mandatory constraint.",
            ParaphraseGroupId = testCase.AmbiguityGroup,
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["equipmentSource"] = "derived-from-sku-names",
                ["duplicateGroup"] = testCase.DuplicateGroup ?? string.Empty,
            },
        };
    }
}
