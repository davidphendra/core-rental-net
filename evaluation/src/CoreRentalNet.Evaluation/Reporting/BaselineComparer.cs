using CoreRentalNet.Evaluation.Evaluation;

namespace CoreRentalNet.Evaluation.Reporting;

/// <summary>Compares two reports by row number, never by collapsing duplicate rows.</summary>
public sealed class BaselineComparer
{
    public RegressionReport Compare(EvaluationReport baseline, EvaluationReport current)
    {
        var currentByRow = current.Cases.ToDictionary(testCase => testCase.RowNumber);
        var baselinePassing = baseline.Cases.Where(testCase => testCase.CasePass).ToArray();

        var regressions = baselinePassing
            .Where(testCase => currentByRow.TryGetValue(testCase.RowNumber, out var currentCase)
                               && !currentCase.CasePass)
            .Select(testCase => new CaseRegression(
                testCase.RowNumber,
                testCase.CaseId,
                testCase.Stability,
                currentByRow[testCase.RowNumber].Stability,
                currentByRow[testCase.RowNumber].FailureCodes))
            .ToArray();

        var newlyPassing = baseline.Cases
            .Where(testCase => !testCase.CasePass
                               && currentByRow.TryGetValue(testCase.RowNumber, out var currentCase)
                               && currentCase.CasePass)
            .Select(testCase => testCase.RowNumber)
            .ToArray();

        return new RegressionReport(
            Comparable: true,
            BaselinePassingRows: baselinePassing.Length,
            Regressions: regressions,
            RegressionRate: baselinePassing.Length == 0
                ? 0
                : regressions.Length / (double)baselinePassing.Length,
            NewlyPassing: newlyPassing);
    }
}
