using System.Globalization;
using CoreRentalNet.Evaluation.Reporting;

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

        Console.WriteLine($"Baseline passing rows: {report.BaselinePassingRows}");
        Console.WriteLine($"Regressions:           {report.Regressions.Count}");
        Console.WriteLine($"Regression rate:       {report.RegressionRate.ToString("P1", CultureInfo.InvariantCulture)}");
        Console.WriteLine($"Newly passing:         {report.NewlyPassing.Count}");

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
