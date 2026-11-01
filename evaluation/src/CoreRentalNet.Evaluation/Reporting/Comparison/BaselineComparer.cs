using AgentEval.Output;
using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;

using CoreRentalNet.Evaluation.Reporting.Html;
using CoreRentalNet.Evaluation.Reporting.Writers;
using CoreRentalNet.Evaluation.Reporting.AgentEval;
namespace CoreRentalNet.Evaluation.Reporting.Comparison;

/// <summary>Compares two reports through AgentEval's comparability decision.</summary>
public sealed class BaselineComparer
{
    public RegressionReport Compare(EvaluationReport baseline, EvaluationReport current)
    {
        if (baseline.Scope != current.Scope || baseline.TotalRows != current.TotalRows)
        {
            return new RegressionReport(
                Comparable: false,
                BaselinePassingRows: 0,
                Regressions: [],
                RegressionRate: 0,
                NewlyPassing: [])
            {
                Reasons =
                [
                    $"The runs are not comparable: baseline is {baseline.Scope} " +
                    $"({baseline.Summary.Rows}/{baseline.TotalRows}), current is " +
                    $"{current.Scope} ({current.Summary.Rows}/{current.TotalRows}).",
                ],
            };
        }

        var projector = new AgentEvalScenarioProjector();
        var comparison = RunComparison.Of(
            projector.Project(baseline),
            projector.Project(current));

        var baselinePassing = baseline.Cases.Count(testCase => testCase.CasePass);

        if (comparison.Verdict == ComparisonVerdict.Incomparable)
        {
            return new RegressionReport(
                Comparable: false,
                BaselinePassingRows: baselinePassing,
                Regressions: [],
                RegressionRate: 0,
                NewlyPassing: [])
            {
                Reasons = comparison.RefusalReasons,
            };
        }

        var baselineById = baseline.Cases.ToDictionary(AgentEvalScenarioProjector.Id);
        var currentById = current.Cases.ToDictionary(AgentEvalScenarioProjector.Id);

        var regressions = comparison.Scenarios
            .Where(scenario => scenario.BaselinePassed && !scenario.CandidatePassed)
            .Select(scenario =>
            {
                var before = baselineById[scenario.ScenarioId];
                var after = currentById[scenario.ScenarioId];

                return new CaseRegression(
                    before.RowNumber,
                    before.CaseId,
                    before.Stability,
                    after.Stability,
                    after.FailureCodes);
            })
            .ToArray();

        var newlyPassing = comparison.Scenarios
            .Where(scenario => !scenario.BaselinePassed && scenario.CandidatePassed)
            .Select(scenario => currentById[scenario.ScenarioId].RowNumber)
            .ToArray();

        return new RegressionReport(
            Comparable: true,
            BaselinePassingRows: baselinePassing,
            Regressions: regressions,
            RegressionRate: baselinePassing == 0
                ? 0
                : regressions.Length / (double)baselinePassing,
            NewlyPassing: newlyPassing)
        {
            SnapshotChanges = AgentEvalSnapshotComparer.Compare(baseline, current),
        };
    }
}
