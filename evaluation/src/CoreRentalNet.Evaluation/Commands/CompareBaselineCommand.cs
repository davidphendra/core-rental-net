using System.Globalization;
using CoreRentalNet.Evaluation.Reporting.Html;
using CoreRentalNet.Evaluation.Reporting.Comparison;
using CoreRentalNet.Evaluation.Reporting.Writers;
using CoreRentalNet.Evaluation.Reporting.AgentEval;

namespace CoreRentalNet.Evaluation.Commands;

/// <summary>Compares the current report with an accepted baseline.</summary>
public static class CompareBaselineCommand
{
    public static async Task<int> RunAsync(
        string[] args,
        CancellationToken cancellationToken = default)
    {
        var baselinePath = args.ElementAtOrDefault(0);
        var currentPath = args.ElementAtOrDefault(1);

        if (baselinePath is null || currentPath is null)
        {
            Console.Error.WriteLine("Usage: compare <baseline.json> <current.json>");
            return 1;
        }

        var baseline = await BaselineReader.ReadAsync(baselinePath, cancellationToken);
        var current = await BaselineReader.ReadAsync(currentPath, cancellationToken);
        var report = new BaselineComparer().Compare(baseline, current);

        if (!report.Comparable)
        {
            Console.Error.WriteLine(
                $"Not comparable: baseline is {baseline.Scope} " +
                $"({baseline.Summary.Rows}/{baseline.TotalRows}), current is " +
                $"{current.Scope} ({current.Summary.Rows}/{current.TotalRows}).");

            foreach (var reason in report.Reasons)
            {
                Console.Error.WriteLine($"  {reason}");
            }

            return 2;
        }

        Console.WriteLine($"Baseline passing rows: {report.BaselinePassingRows}");
        Console.WriteLine($"Regressions:           {report.Regressions.Count}");
        Console.WriteLine($"Regression rate:       {report.RegressionRate.ToString("P1", CultureInfo.InvariantCulture)}");
        Console.WriteLine($"Newly passing:         {report.NewlyPassing.Count}");
        Console.WriteLine($"Snapshot changes:      {report.SnapshotChanges.Count}");

        foreach (var drift in report.SnapshotChanges)
        {
            Console.WriteLine(
                $"SNAPSHOT row {drift.RowNumber} {drift.CaseId}: " +
                $"{string.Join("; ", drift.Differences)}");
        }

        foreach (var regression in report.Regressions)
        {
            Console.WriteLine(
                $"REGRESSION row {regression.RowNumber} {regression.CaseId}: " +
                $"{regression.BaselineStability} -> {regression.CurrentStability} " +
                $"[{string.Join("; ", regression.FailureCodes)}]");
        }

        return report.HasRegressions ? 1 : 0;
    }
}
