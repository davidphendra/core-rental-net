using System.Text.Json;
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

/// <summary>A failure row explains the failure, and a record written before these fields existed still renders.</summary>
public sealed class ReportCompletenessTests
{
    [Fact]
    public void A_failure_row_shows_the_query_the_failed_metric_and_what_retrieval_returned()
    {
        var html = new HtmlRunReport().Render(Record());

        html.Should().Contain("one desk and two monitors")
            .And.Contain("constraint_compliance")
            .And.Contain("MONB086Q776F, DKB");
    }

    [Fact]
    public void Judge_decisions_appear_with_their_explanation()
    {
        var record = Record() with
        {
            Judgements = [new CaseJudgement("TST-001", "relevance", 4.5, "The answer names both monitors.", "gpt-oss-20b")],
        };

        new HtmlRunReport().Render(record).Should()
            .Contain("Judge decisions").And.Contain("gpt-oss-20b").And.Contain("names both monitors");
    }

    [Fact]
    public void No_judge_means_no_judge_section()
        => new HtmlRunReport().Render(Record()).Should().NotContain("Judge decisions");

    [Fact]
    public void A_record_written_before_the_new_fields_existed_still_loads_and_renders()
    {
        const string legacy = """
        {"runId":"old","dataset":"Validation","datasetVersion":"v1","datasetSha256":"aa",
         "cases":[{"caseId":"VAL-001","category":"normal","expected":"Success","actual":"Rejected",
                   "correct":false,"recommendedSkus":[],"failureCodes":["JUDGE_REJECTED"]}],
         "metrics":[]}
        """;

        var record = JsonSerializer.Deserialize<EvaluationRunRecord>(legacy, EvaluationJson.Options)!;

        record.Cases.Single().Query.Should().BeEmpty();
        new HtmlRunReport().Render(record).Should().Contain("VAL-001").And.Contain("JUDGE_REJECTED");
    }

    private static EvaluationRunRecord Record() => new()
    {
        RunId = "run",
        Dataset = DatasetName.Validation,
        DatasetVersion = "v1",
        DatasetSha256 = "aa",
        TotalCases = 1,
        Cases =
        [
            new CaseRunRecord("TST-001", "invalid", RagOutcome.Rejected, RagOutcome.Success, false,
                ["MONB086Q776F"], ["CONSTRAINT_VIOLATION"])
            {
                Query = "one desk and two monitors",
                RetrievedSkus = ["MONB086Q776F", "DKB"],
                FailedMetric = "constraint_compliance",
            },
        ],
    };
}
