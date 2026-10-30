using AgentEval.Core;
using AgentEval.Exporters;
using AgentEval.Models;

namespace CoreRentalNet.Evaluation.Reporting;

/// <summary>Writes AgentEval's standard report formats beside the domain report.</summary>
public static class AgentEvalReportExporter
{
    public static async Task WriteAsync(
        EvaluationReport report,
        string reportPath,
        IReadOnlyList<string> formats,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(reportPath);
        ArgumentNullException.ThrowIfNull(formats);

        foreach (var format in formats.Select(Parse))
        {
            var exporter = ResultExporterFactory.Create(format);
            var path = Path.ChangeExtension(reportPath, $".agenteval{exporter.FileExtension}");
            var directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var stream = File.Create(path);
            await exporter.ExportAsync(report, stream, cancellationToken);
        }
    }

    public static ExportFormat Parse(string value)
        => value.Trim().ToLowerInvariant() switch
        {
            "json" => ExportFormat.Json,
            "junit" or "xml" => ExportFormat.Junit,
            "markdown" or "md" => ExportFormat.Markdown,
            "trx" => ExportFormat.Trx,
            "csv" => ExportFormat.Csv,
            _ => throw new ArgumentException(
                $"Unknown export format '{value}'. Use json, junit, markdown, trx or csv."),
        };
}
