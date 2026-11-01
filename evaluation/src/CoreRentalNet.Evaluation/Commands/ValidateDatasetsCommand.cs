using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;

namespace CoreRentalNet.Evaluation.Commands;

/// <summary>Loads and validates the three datasets without running the agent or spending a model call.</summary>
public static class ValidateDatasetsCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        var paths = DatasetPaths.Default;
        var loader = new DatasetLoader();
        var validator = new DatasetValidator();
        var datasets = new List<RagEvaluationDataset>();

        foreach (var name in Enum.GetValues<DatasetName>())
        {
            var dataset = await loader.LoadAsync(name, paths.For(name)).ConfigureAwait(false);
            datasets.Add(dataset);
            Report(dataset, validator.Validate(dataset));
        }

        var across = validator.ValidateAcrossDatasets(datasets);
        foreach (var error in across.Errors)
        {
            Console.WriteLine($"  error   {error}");
        }

        var failed = datasets.Count == 0 || across.Errors.Count > 0;
        Console.WriteLine(failed ? "Datasets: FAILED" : "Datasets: OK");
        return failed ? 1 : 0;
    }

    private static void Report(RagEvaluationDataset dataset, DatasetValidation validation)
    {
        Console.WriteLine(
            $"{dataset.Name,-11} cases={dataset.Cases.Count,4}  bytes={dataset.SizeInBytes,7}  " +
            $"sha256={dataset.Sha256[..12]}  excluded={dataset.Exclusions.Count}  lockbox={dataset.IsLockbox}");

        foreach (var warning in validation.Warnings.Take(5))
        {
            Console.WriteLine($"  warning {warning}");
        }

        foreach (var error in validation.Errors.Take(20))
        {
            Console.WriteLine($"  error   {error}");
        }
    }
}
