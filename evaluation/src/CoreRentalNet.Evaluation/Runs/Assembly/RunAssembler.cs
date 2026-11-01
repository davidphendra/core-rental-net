using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;
using CoreRentalNet.Evaluation.Evaluators.Judges;
using CoreRentalNet.Evaluation.Evaluators.Correctness;
using CoreRentalNet.Evaluation.Evaluators.Shared;
using CoreRentalNet.Evaluation.Metrics.Outcome;
using CoreRentalNet.Evaluation.Metrics.Retrieval;
using CoreRentalNet.Evaluation.Metrics.Statistics;
using CoreRentalNet.Evaluation.Metrics.Rules;
using CoreRentalNet.Evaluation.Metrics.Thresholds;

using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
namespace CoreRentalNet.Evaluation.Runs.Assembly;

/// <summary>Turns one dataset and its runs into the record a report and a comparison read.</summary>
/// <remarks>
/// The assembler grades each case once and derives every metric from that same grading, so a case cannot be
/// correct for answer quality and wrong for outcome accuracy in the same report.
/// </remarks>
public sealed class RunAssembler
{
    public EvaluationRunRecord Assemble(
        RagEvaluationDataset dataset,
        IReadOnlyList<RagRunResult> runs,
        RunIdentity identity,
        IReadOnlyList<EvaluationMetricResult>? importedMetrics = null,
        IReadOnlyList<CaseJudgement>? judgements = null)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        ArgumentNullException.ThrowIfNull(runs);
        ArgumentNullException.ThrowIfNull(identity);

        var byCase = runs.ToDictionary(run => run.CaseId, StringComparer.Ordinal);
        var graded = dataset.Cases.Select(testCase => Grade(testCase, byCase)).ToArray();
        var outcomes = graded
            .Where(entry => entry.Run is not null)
            .Select(entry => new OutcomeRecord(
                entry.Record.CaseId,
                entry.Record.Category,
                entry.Record.Expected,
                entry.Run!.Outcome))
            .ToArray();

        return new EvaluationRunRecord
        {
            RunId = identity.RunId,
            Dataset = dataset.Name,
            DatasetVersion = dataset.Version,
            DatasetSha256 = dataset.Sha256,
            IsLockbox = dataset.IsLockbox,
            AppVersion = identity.AppVersion,
            GeneratorModel = identity.GeneratorModel,
            JudgeModel = identity.JudgeModel,
            RetrievalConfiguration = identity.RetrievalConfiguration,
            StartedAtUtc = identity.StartedAtUtc,
            DurationMs = identity.DurationMs,
            TotalCases = dataset.Cases.Count,
            Correct = graded.Count(entry => entry.Record.Correct),
            Incorrect = graded.Count(entry => entry.Record.Actual is RagOutcome.Success or RagOutcome.Rejected or RagOutcome.Unavailable
                && !entry.Record.Correct),
            Indeterminate = graded.Count(entry => entry.Record.Actual == RagOutcome.Indeterminate),
            Metrics = [.. Metrics(graded, outcomes), .. importedMetrics ?? []],
            Judgements = judgements ?? [],
            Cases = [.. graded.Select(entry => entry.Record)],
            Exclusions = dataset.Exclusions,
        };
    }

    private static IReadOnlyList<EvaluationMetricResult> Metrics(
        IReadOnlyList<Graded> graded,
        IReadOnlyList<OutcomeRecord> outcomes)
    {
        var determinate = outcomes.Where(record => record.IsDeterminate).ToArray();
        var answerable = determinate.Where(record => record.Expected == RagOutcome.Success).ToArray();
        var abstainable = determinate.Where(record => record.Expected != RagOutcome.Success).ToArray();
        var successes = graded.Where(entry => entry.Verdict is not null).ToArray();

        return
        [
            MetricEstimate.FromValues(
                "outcome_accuracy",
                [.. determinate.Select(record => record.IsCorrect ? 1d : 0d)],
                outcomes.Count - determinate.Length),
            MetricEstimate.FromValues(
                "false_acceptance",
                [.. abstainable.Select(record => record.Actual == RagOutcome.Success ? 1d : 0d)],
                outcomes.Count - abstainable.Length),
            MetricEstimate.FromValues(
                "false_rejection",
                [.. answerable.Select(record => record.Actual == RagOutcome.Success ? 0d : 1d)],
                outcomes.Count - answerable.Length),
            MetricEstimate.FromValues(
                "answer_correctness",
                [.. successes.Select(entry => entry.Verdict == AnswerVerdict.Correct ? 1d : 0d)],
                graded.Count - successes.Length),
            MetricEstimate.FromValues(
                "substitution_rate",
                [.. successes.Select(entry => entry.Verdict == AnswerVerdict.Substituted ? 1d : 0d)],
                graded.Count - successes.Length),
            MetricEstimate.FromValues(
                "constraint_compliance",
                [.. graded.Select(entry => entry.Record.FailureCodes.Contains("CONSTRAINT_VIOLATION") ? 0d : 1d)],
                0),
        ];
    }

    private static Graded Grade(RagEvaluationCase testCase, IReadOnlyDictionary<string, RagRunResult> byCase)
    {
        if (!byCase.TryGetValue(testCase.Id, out var run))
        {
            return new Graded(
                testCase,
                null,
                null,
                new CaseRunRecord(testCase.Id, testCase.Category, testCase.ExpectedOutcome,
                    RagOutcome.Indeterminate, false, [], ["RUN_MISSING"]));
        }

        var verdict = AnswerCorrectness.Evaluate(testCase, run);
        var rules = ConstraintRuleBreakdown.Evaluate(run.RecommendedLines);
        var failures = new List<string>(run.FailureCodes);

        if (!rules.Compliant)
        {
            failures.Add("CONSTRAINT_VIOLATION");
        }

        return new Graded(
            testCase,
            run,
            testCase.ExpectedOutcome == RagOutcome.Success ? verdict : null,
            new CaseRunRecord(
                testCase.Id,
                testCase.Category,
                testCase.ExpectedOutcome,
                run.Outcome,
                run.Outcome == testCase.ExpectedOutcome,
                [.. run.RecommendedSkus],
                failures)
            {
                Query = testCase.Query,
                RetrievedSkus = run.RetrievedSkus,
                FailedMetric = FirstFailure(testCase, run, verdict, rules),
            });
    }

    /// <summary>The first rule or metric the case broke, in the order a reader would check them.</summary>
    private static string? FirstFailure(
        RagEvaluationCase testCase,
        RagRunResult run,
        AnswerVerdict verdict,
        ConstraintRuleReport rules)
    {
        if (!rules.Compliant)
        {
            return "constraint_compliance";
        }

        if (run.Outcome != testCase.ExpectedOutcome)
        {
            return "outcome_accuracy";
        }

        return verdict == AnswerVerdict.Miss ? "answer_correctness" : null;
    }

    private sealed record Graded(
        RagEvaluationCase? Case,
        RagRunResult? Run,
        AnswerVerdict? Verdict,
        CaseRunRecord Record);
}
