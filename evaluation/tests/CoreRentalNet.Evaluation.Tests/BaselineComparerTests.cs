using AwesomeAssertions;
using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Evaluation;
using CoreRentalNet.Evaluation.Reporting;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests;

public sealed class BaselineComparerTests
{
    [Fact]
    public void A_case_that_passed_before_and_fails_now_is_a_regression()
    {
        var baseline = Report(Case(1, pass: true));
        var current = Report(Case(1, pass: false));

        var report = new BaselineComparer().Compare(baseline, current);

        report.Regressions.Should().ContainSingle();
        report.Regressions[0].RowNumber.Should().Be(1);
        report.RegressionRate.Should().Be(1);
    }

    [Fact]
    public void A_case_that_failed_before_and_passes_now_is_newly_passing()
    {
        var baseline = Report(Case(1, pass: false));
        var current = Report(Case(1, pass: true));

        var report = new BaselineComparer().Compare(baseline, current);

        report.NewlyPassing.Should().ContainSingle().Which.Should().Be(1);
        report.RegressionRate.Should().Be(0);
    }

    [Fact]
    public void A_duplicate_row_is_compared_by_its_own_row_number()
    {
        var baseline = Report(Case(19, pass: true), Case(59, pass: true));
        var current = Report(Case(19, pass: true), Case(59, pass: false));

        var report = new BaselineComparer().Compare(baseline, current);

        report.Regressions.Should().ContainSingle().Which.RowNumber.Should().Be(59);
    }

    [Fact]
    public void Reports_with_different_scopes_are_not_comparable()
    {
        var baseline = Report(EvaluationScope.Tiny, Case(1, pass: true));
        var current = Report(EvaluationScope.All, Case(1, pass: true));

        var report = new BaselineComparer().Compare(baseline, current);

        report.Comparable.Should().BeFalse();
    }

    private static EvaluationReport Report(params CaseResult[] cases)
        => Report(EvaluationScope.All, cases);

    private static EvaluationReport Report(
        EvaluationScope scope,
        params CaseResult[] cases)
        => new(
            Cases: cases,
            Metrics: [],
            Summary: new EvaluationSummary(
                Rows: cases.Length,
                Executions: cases.Sum(testCase => testCase.Repetitions.Count),
                CasePass: cases.Count(testCase => testCase.CasePass),
                CasePassRate: cases.Length == 0 ? 0 : cases.Count(testCase => testCase.CasePass) / (double)cases.Length,
                MetricPassRates: new Dictionary<string, double>(),
                Stability: new Dictionary<string, int>()))
        {
            Scope = scope,
            TotalRows = 200,
        };

    private static CaseResult Case(int row, bool pass)
        => new(
            RowNumber: row,
            CaseId: $"TS-{row:000}",
            Sentence: "sentence",
            Repetitions:
            [
                new RepetitionResult(
                    Repetition: 1,
                    Metrics: MetricNames.All.ToDictionary(metric => metric, _ => pass),
                    FailureCodes: pass ? [] : ["ANSWER_MISMATCH"],
                    AnswerF1: pass ? 1 : 0,
                    RetrievalRecall: pass ? 1 : 0),
                new RepetitionResult(
                    Repetition: 2,
                    Metrics: MetricNames.All.ToDictionary(metric => metric, _ => pass),
                    FailureCodes: pass ? [] : ["ANSWER_MISMATCH"],
                    AnswerF1: pass ? 1 : 0,
                    RetrievalRecall: pass ? 1 : 0),
                new RepetitionResult(
                    Repetition: 3,
                    Metrics: MetricNames.All.ToDictionary(metric => metric, _ => pass),
                    FailureCodes: pass ? [] : ["ANSWER_MISMATCH"],
                    AnswerF1: pass ? 1 : 0,
                    RetrievalRecall: pass ? 1 : 0),
            ],
            CasePass: pass,
            Stability: pass ? "3/3" : "0/3",
            FailureCodes: pass ? [] : ["ANSWER_MISMATCH"],
            MeanAnswerF1: pass ? 1 : 0,
            MeanRetrievalRecall: pass ? 1 : 0);
}
