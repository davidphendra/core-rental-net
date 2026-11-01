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

/// <summary>Prints the deltas between two stored runs, after stating whether they are comparable.</summary>
public static class CompareRunsCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: compare-runs <baseline-run.json> <current-run.json>");
            return 1;
        }

        var baseline = await ReadAsync(args[0]).ConfigureAwait(false);
        var current = await ReadAsync(args[1]).ConfigureAwait(false);
        var report = RunComparer.Compare(baseline, current);

        foreach (var warning in report.Incompatibilities)
        {
            Console.WriteLine($"warning {warning}");
        }

        foreach (var delta in report.Metrics)
        {
            Console.WriteLine($"{delta.Metric,-24} {Show(delta.Baseline),8} -> {Show(delta.Current),8}  delta {Show(delta.Delta)}");
        }

        return 0;
    }

    private static string Show(double? value) => value is null ? "n/a" : value.Value.ToString("0.000");

    private static async Task<EvaluationRunRecord> ReadAsync(string path)
        => JsonSerializer.Deserialize<EvaluationRunRecord>(await File.ReadAllTextAsync(path).ConfigureAwait(false), EvaluationJson.Options)
           ?? throw new InvalidOperationException($"The run record at '{path}' was empty.");
}
