using System.Text.Json;
using CoreRentalNet.Evaluation.Evaluation;

namespace CoreRentalNet.Evaluation.Reporting;

/// <summary>Reads a committed baseline report.</summary>
public static class BaselineReader
{
    public static async Task<EvaluationReport> ReadAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var json = await File.ReadAllTextAsync(path, cancellationToken);

        return JsonSerializer.Deserialize<EvaluationReport>(json, EvaluationJson.Options)
               ?? throw new InvalidOperationException($"The baseline at '{path}' was not readable.");
    }
}
