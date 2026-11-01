using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;

namespace CoreRentalNet.Evaluation.Datasets.Validation;

/// <summary>Checks a dataset against the annotation contract and the mandatory workspace rules.</summary>
/// <remarks>
/// The rules are checked here rather than in an evaluator because a dataset that contradicts them is not a
/// measurement problem: a case marked <c>success</c> whose quantities break a limit can never be graded fairly,
/// whatever the agent answers.
/// </remarks>
public sealed class DatasetValidator
{
    private static readonly HashSet<string> Categories = new(StringComparer.Ordinal)
    {
        "normal", "paraphrase", "complex", "boundary", "invalid",
        "ambiguous", "unavailable", "adversarial", "generalization",
    };

    public DatasetValidation Validate(RagEvaluationDataset dataset)
    {
        ArgumentNullException.ThrowIfNull(dataset);

        var errors = new List<string>();
        var warnings = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var testCase in dataset.Cases)
        {
            ValidateCase(dataset.Name, testCase, seen, errors, warnings);
        }

        if (dataset.Cases.Count == 0)
        {
            errors.Add($"{dataset.Name}: the dataset has no cases.");
        }

        foreach (var exclusion in dataset.Exclusions)
        {
            warnings.Add($"{dataset.Name}: {exclusion.Id} excluded - {exclusion.Reason}");
        }

        return new DatasetValidation(errors.Count == 0, errors, warnings);
    }

    /// <summary>Identifiers must be unique across datasets, because traces and reports key on them.</summary>
    public DatasetValidation ValidateAcrossDatasets(IReadOnlyList<RagEvaluationDataset> datasets)
    {
        ArgumentNullException.ThrowIfNull(datasets);

        var owners = new Dictionary<string, string>(StringComparer.Ordinal);
        var errors = new List<string>();

        foreach (var dataset in datasets)
        {
            foreach (var testCase in dataset.Cases)
            {
                if (owners.TryGetValue(testCase.Id, out var owner))
                {
                    errors.Add(
                        $"{testCase.Id}: used by {owner} and {dataset.Name}; case ids must be unique across datasets.");
                }
                else
                {
                    owners[testCase.Id] = dataset.Name.ToString();
                }
            }
        }

        return errors.Count == 0 ? DatasetValidation.Valid() : DatasetValidation.Invalid(errors);
    }

    private static void ValidateCase(
        DatasetName name,
        RagEvaluationCase testCase,
        HashSet<string> seen,
        List<string> errors,
        List<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(testCase.Id))
        {
            errors.Add($"{name}: a case has no id.");
            return;
        }

        if (!seen.Add(testCase.Id))
        {
            errors.Add($"{testCase.Id}: duplicate id within {name}.");
        }

        if (!Categories.Contains(testCase.Category) && !IsAllowedUncategorised(name, testCase.Category))
        {
            errors.Add($"{testCase.Id}: unknown category '{testCase.Category}'.");
        }

        if (string.IsNullOrWhiteSpace(testCase.Query))
        {
            errors.Add($"{testCase.Id}: the query is empty.");
        }

        ValidateOutcome(testCase, errors);
        ValidateFacts(testCase, warnings);
    }

    private static bool IsAllowedUncategorised(DatasetName name, string category)
        => name == DatasetName.Development && string.Equals(category, "uncategorized", StringComparison.Ordinal);

    private static void ValidateOutcome(RagEvaluationCase testCase, List<string> errors)
    {
        var compliant = testCase.ExpectedEquipment.SatisfiesMandatoryConstraints;

        if (testCase.ExpectedOutcome == RagOutcome.Indeterminate)
        {
            errors.Add($"{testCase.Id}: the expected outcome must be success, rejected or unavailable.");
            return;
        }

        if (testCase.ExpectedOutcome == RagOutcome.Success && !compliant)
        {
            errors.Add($"{testCase.Id}: marked success but the equipment breaks a mandatory limit.");
        }

        if (testCase.ExpectedOutcome == RagOutcome.Rejected && compliant)
        {
            errors.Add($"{testCase.Id}: marked rejected but the equipment satisfies every limit.");
        }

        if (testCase.ExpectedOutcome == RagOutcome.Unavailable && !compliant)
        {
            errors.Add($"{testCase.Id}: marked unavailable but the equipment itself is invalid.");
        }

        if (testCase.ExpectedOutcome != RagOutcome.Success && testCase.ExpectedWorkspaceIds.Count > 0)
        {
            errors.Add($"{testCase.Id}: only a success case may name expected SKUs.");
        }
    }

    private static void ValidateFacts(RagEvaluationCase testCase, List<string> warnings)
    {
        if (testCase.ExpectedFacts.Any(string.IsNullOrWhiteSpace))
        {
            warnings.Add($"{testCase.Id}: an annotated fact is blank.");
        }

        if (testCase.RelevantWorkspaceIds.Count == 0 && testCase.ExpectedOutcome == RagOutcome.Success)
        {
            warnings.Add($"{testCase.Id}: no relevance labels, so retrieval metrics are not evaluable.");
        }
    }
}
