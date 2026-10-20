using System.Text.Json;
using CoreRentalNet.Evaluation.Evaluation;

namespace CoreRentalNet.Evaluation.Reporting;

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
