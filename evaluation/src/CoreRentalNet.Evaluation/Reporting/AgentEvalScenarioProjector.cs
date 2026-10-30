using AgentEval.Output;
using CoreRentalNet.Evaluation.Evaluation;

namespace CoreRentalNet.Evaluation.Reporting;

/// <summary>Projects a report's cases into AgentEval's comparability model.</summary>
public sealed class AgentEvalScenarioProjector
{
    public IReadOnlyList<ScenarioResult> Project(EvaluationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var hasJudge = report.Summary.JudgeConfigured;
        var metricsByCase = report.Metrics
            .GroupBy(row => row.RowNumber)
            .ToDictionary(group => group.Key, group => group.ToArray());

        return report.Cases
            .Select(testCase => Project(testCase, metricsByCase.GetValueOrDefault(testCase.RowNumber) ?? [], hasJudge, report))
            .ToArray();
    }

    public static string Id(CaseResult testCase) => $"{testCase.RowNumber}:{testCase.CaseId}";

    private static ScenarioResult Project(
        CaseResult testCase,
        IReadOnlyList<MetricRow> rows,
        bool hasJudge,
        EvaluationReport report)
    {
        var measured = rows.Where(row => row.Measured && row.Value is not null).ToArray();
        var metrics = measured
            .GroupBy(row => row.Metric, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Average(row => row.Value!.Value),
                StringComparer.Ordinal);

        var score = measured.Length > 0
            ? measured.Average(row => row.Value!.Value)
            : testCase.CasePass ? 100 : 0;

        var facts = new ComparabilityFacts(EvaluationPolicy.Version, "1")
        {
            EffectiveBar = 70,
            EvaluationPolicyDigest = report.PolicyDigest ?? "legacy",
            InstrumentKind = hasJudge ? EvaluationInstrumentKind.Model : EvaluationInstrumentKind.Deterministic,
            Judge = hasJudge
                ? new JudgeFingerprint("evaluation-judge", RubricDigest: null, JudgeSubjectRelation.Unknown)
                : null,
        };

        return new ScenarioResult(
            Id: Id(testCase),
            Name: testCase.CaseId,
            Input: testCase.Sentence,
            Output: testCase.ActualOutput,
            Passed: testCase.CasePass,
            Score: score,
            Metrics: metrics,
            Assertions: [],
            Duration: null,
            EstimatedCost: null)
        {
            StimulusHash = StimulusHash.Of(testCase.Sentence),
            Comparability = facts,
            Measurement = measured.Length == 0 ? ScenarioMeasurement.NotMeasured : null,
        };
    }
}
