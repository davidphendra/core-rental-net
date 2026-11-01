using System.Text.Json;
using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;

using CoreRentalNet.Evaluation.Reporting.Html;
using CoreRentalNet.Evaluation.Reporting.Comparison;
using CoreRentalNet.Evaluation.Reporting.AgentEval;
namespace CoreRentalNet.Evaluation.Reporting.Writers;

/// <summary>Writes the machine-readable report.</summary>
public static class JsonReportWriter
{
    private static readonly JsonSerializerOptions Options = new(EvaluationJson.Options)
    {
        WriteIndented = true,
    };

    public static async Task WriteAsync(
        EvaluationReport report,
        string path,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(
            path,
            JsonSerializer.Serialize(report, Options),
            cancellationToken);
    }
}
