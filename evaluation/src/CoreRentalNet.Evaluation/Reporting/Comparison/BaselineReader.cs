using System.Text.Json;
using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;

using CoreRentalNet.Evaluation.Reporting.Html;
using CoreRentalNet.Evaluation.Reporting.Writers;
using CoreRentalNet.Evaluation.Reporting.AgentEval;
namespace CoreRentalNet.Evaluation.Reporting.Comparison;

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
