using System.Text.Json;
using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;

using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;
namespace CoreRentalNet.Evaluation.Runs.Storage;

/// <summary>Finds the run a comparison should be made against, or says there is none.</summary>
/// <remarks>
/// Resolution is separated from the command so it can be proven without executing an agent: 'previous' means the
/// latest earlier run of the same dataset, and an unknown target is a null, not an exception.
/// </remarks>
public sealed class RunBaselineLocator(string reportsDirectory)
{
    public EvaluationRunRecord? Resolve(string target, DatasetName dataset, string currentRunId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentRunId);

        if (string.Equals(target, "previous", StringComparison.OrdinalIgnoreCase))
        {
            return Directory.Exists(reportsDirectory)
                ? All()
                    .Where(record => record.Dataset == dataset && record.RunId != currentRunId)
                    .OrderBy(record => record.StartedAtUtc)
                    .LastOrDefault()
                : null;
        }

        var path = File.Exists(target) ? target : Path.Combine(reportsDirectory, target, "run.json");
        return File.Exists(path) ? Read(path) : null;
    }

    private IEnumerable<EvaluationRunRecord> All()
        => Directory.EnumerateFiles(reportsDirectory, "run.json", SearchOption.AllDirectories)
            .Select(Read)
            .OfType<EvaluationRunRecord>();

    private static EvaluationRunRecord? Read(string path)
        => JsonSerializer.Deserialize<EvaluationRunRecord>(File.ReadAllText(path), EvaluationJson.Options);
}
