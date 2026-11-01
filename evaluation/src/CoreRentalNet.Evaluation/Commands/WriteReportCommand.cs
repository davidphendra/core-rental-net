using System.Text.Json;
using CoreRentalNet.Evaluation.Reporting.Html;
using CoreRentalNet.Evaluation.Reporting.Comparison;
using CoreRentalNet.Evaluation.Reporting.Writers;
using CoreRentalNet.Evaluation.Reporting.AgentEval;
using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;

namespace CoreRentalNet.Evaluation.Commands;

/// <summary>Renders a stored run as HTML beside the run record, spending no model call.</summary>
public static class WriteReportCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: report <path-to-run.json>");
            return 1;
        }

        var path = args[0];

        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"No run record at '{path}'.");
            return 1;
        }

        var record = JsonSerializer.Deserialize<EvaluationRunRecord>(
            await File.ReadAllTextAsync(path),
            EvaluationJson.Options);

        if (record is null)
        {
            Console.Error.WriteLine($"The run record at '{path}' was empty.");
            return 1;
        }

        var html = new HtmlRunReport().Render(record);
        var output = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, "report.html");
        await File.WriteAllTextAsync(output, html);

        Console.WriteLine($"report  {output}");
        Console.WriteLine($"metrics {record.Metrics.Count}  cases {record.TotalCases}  lockbox {record.IsLockbox}");
        return 0;
    }
}
