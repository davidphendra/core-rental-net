using System.Text.Json;

using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;
namespace CoreRentalNet.Evaluation.Runs.Storage;

/// <summary>Writes each run to its own directory, holding the report beside the evidence.</summary>
/// <remarks>
/// A run id may not be reused: an existing directory is an error rather than an overwrite. Otherwise a second
/// evaluation could silently replace the record a report already cites.
/// </remarks>
public sealed class JsonEvaluationRunStore(string rootDirectory) : IEvaluationRunStore
{
    public async Task<string> SaveAsync(
        EvaluationRunRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentException.ThrowIfNullOrWhiteSpace(record.RunId);

        var directory = Path.Combine(rootDirectory, record.RunId);

        if (Directory.Exists(directory))
        {
            throw new InvalidOperationException(
                $"Run '{record.RunId}' already exists at '{directory}'. Choose a new run id.");
        }

        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "run.json");
        var json = JsonSerializer.Serialize(record, EvaluationJson.Options);
        await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);

        return path;
    }
}
