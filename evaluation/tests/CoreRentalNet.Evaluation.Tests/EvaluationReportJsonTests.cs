using System.Text.Json;
using AwesomeAssertions;
using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Evaluation;
using CoreRentalNet.Evaluation.Reporting.Bias;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests;

public sealed class EvaluationReportJsonTests
{
    [Fact]
    public void The_report_records_the_scope_as_a_name_and_the_full_row_count()
    {
        var report = new EvaluationReport(
            Cases: [],
            Metrics: [],
            Summary: new EvaluationSummary(
                Rows: 0,
                Executions: 0,
                CasePass: 0,
                CasePassRate: 0,
                MetricPassRates: new Dictionary<string, double>(),
                Stability: new Dictionary<string, int>()))
        {
            Scope = EvaluationScope.Tiny,
            TotalRows = 200,
        };

        var json = JsonSerializer.Serialize(report, EvaluationJson.Options);

        json.Should().Contain("\"scope\":\"Tiny\"");
        json.Should().Contain("\"totalRows\":200");
    }

    [Fact]
    public void The_report_records_a_bias_section_list()
    {
        var report = new EvaluationReport([], [], new EvaluationSummary(0, 0, 0, 0, new Dictionary<string, double>(), new Dictionary<string, int>()))
        {
            Bias = new BiasReport(
            [
                BiasMetric.NotApplicable(BiasSectionName.Fairness, "Demographic Parity Difference", "No labels."),
            ]),
        };

        var json = JsonSerializer.Serialize(report, EvaluationJson.Options);

        json.Should().Contain("\"bias\"");
        json.Should().Contain("\"Demographic Parity Difference\"");
    }
}
