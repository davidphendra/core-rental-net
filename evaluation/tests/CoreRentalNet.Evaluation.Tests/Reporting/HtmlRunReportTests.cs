using AwesomeAssertions;
using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;
using CoreRentalNet.Evaluation.Reporting.Html;
using CoreRentalNet.Evaluation.Reporting.Comparison;
using CoreRentalNet.Evaluation.Reporting.Writers;
using CoreRentalNet.Evaluation.Reporting.AgentEval;
using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests.Reporting;

/// <summary>The report is self-contained and never renders data as markup.</summary>
public sealed class HtmlRunReportTests
{
    [Fact]
    public void The_page_carries_the_metric_dashboard_and_its_sample_sizes()
    {
        var html = new HtmlRunReport().Render(Record());

        html.Should().StartWith("<!doctype html>").And.Contain("outcome_accuracy").And.Contain("<td>40</td>");
        html.Should().NotContain("<script").And.NotContain("http://").And.NotContain("https://");
    }

    [Fact]
    public void A_metric_without_an_interval_says_so_instead_of_printing_zero()
    {
        var html = new HtmlRunReport().Render(Record());

        html.Should().Contain("not available");
    }

    [Fact]
    public void Case_text_is_encoded_rather_than_rendered()
    {
        var record = Record() with
        {
            Cases = [new CaseRunRecord("TST-<b>1</b>", "normal", RagOutcome.Success, RagOutcome.Rejected, false, [], ["X"])],
        };

        new HtmlRunReport().Render(record).Should().Contain("TST-&lt;b&gt;1&lt;/b&gt;");
    }

    [Fact]
    public void A_lockbox_run_is_marked_as_one()
    {
        new HtmlRunReport().Render(Record()).Should().Contain("lockbox");
    }

    private static EvaluationRunRecord Record() => new()
    {
        RunId = "run-1",
        Dataset = DatasetName.Validation,
        DatasetVersion = "v1",
        DatasetSha256 = "1c7d235f1098aa",
        AppVersion = "0.1.0",
        GeneratorModel = "Qwen3-Coder-30B-A3B-Instruct",
        JudgeModel = "gpt-oss-20b",
        TotalCases = 1,
        Metrics =
        [
            new EvaluationMetricResult("outcome_accuracy", 0.5, 40, 0, 0.95, 0.35, 0.65, "evaluable"),
            new EvaluationMetricResult("answer_correctness", null, 1, 3, 0.95, null, null, "insufficient sample"),
        ],
        Cases = [new CaseRunRecord("TST-001", "normal", RagOutcome.Success, RagOutcome.Success, true, ["DSKB08XN4JDR"], [])],
    };
}
