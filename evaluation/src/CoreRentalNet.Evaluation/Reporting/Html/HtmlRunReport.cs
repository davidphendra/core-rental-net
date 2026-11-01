using System.Net;
using System.Text;
using CoreRentalNet.Evaluation.Analysis.Category;
using CoreRentalNet.Evaluation.Analysis.Generalization;
using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;

using CoreRentalNet.Evaluation.Reporting.Comparison;
using CoreRentalNet.Evaluation.Reporting.Writers;
using CoreRentalNet.Evaluation.Reporting.AgentEval;
namespace CoreRentalNet.Evaluation.Reporting.Html;

/// <summary>Renders one run as a self-contained HTML page that opens from disk.</summary>
/// <remarks>
/// No script, no stylesheet link, no CDN: a report attached to a review must render identically offline and in a
/// year. Every value is encoded, because a case query is user text and the report is not a template engine.
/// </remarks>
public sealed class HtmlRunReport
{
    public string Render(EvaluationRunRecord run)
    {
        ArgumentNullException.ThrowIfNull(run);

        var html = new StringBuilder();
        html.AppendLine("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\">");
        html.AppendLine("<title>Workspace suggestion evaluation</title>");
        html.AppendLine(Style);
        html.AppendLine("</head><body>");
        html.AppendLine(Header(run));
        html.AppendLine(Metrics(run));
        html.AppendLine(Categories(run));
        html.AppendLine(Judges(run));
        html.AppendLine(Failures(run));
        html.AppendLine("</body></html>");
        return html.ToString();
    }

    private static string Header(EvaluationRunRecord run)
        => $"""
           <h1>Workspace suggestion evaluation</h1>
           <p class="meta">run <b>{E(run.RunId)}</b> · dataset <b>{E(run.Dataset.ToString())}</b>
           {Lockbox(run)} · {run.TotalCases} cases · started {run.StartedAtUtc:u}</p>
           <p class="meta">dataset sha256 <code>{E(Short(run.DatasetSha256))}</code> ·
           generator <code>{E(run.GeneratorModel)}</code> · judge <code>{E(run.JudgeModel)}</code> ·
           app <code>{E(run.AppVersion)}</code></p>
           """;

    private static string Lockbox(EvaluationRunRecord run)
        => run.IsLockbox ? "· <b class=\"lockbox\">lockbox</b>" : string.Empty;

    private static string Metrics(EvaluationRunRecord run)
    {
        var rows = run.Metrics.Select(metric => $"""
            <tr class="{Status(metric)}"><td>{E(metric.Metric)}</td><td>{Number(metric.Score)}</td>
            <td>{Interval(metric)}</td><td>{metric.SampleSize}</td><td>{metric.Excluded}</td>
            <td>{E(metric.Status)}</td><td>{Verdict(metric)}</td></tr>
            """);

        return Table("Metric dashboard", "Metric;Score;95% interval;n;excluded;status;verdict", rows);
    }

    private static string Categories(EvaluationRunRecord run)
    {
        var categories = CategoryMetricPerformance.Evaluate(run);
        var columns = categories.SelectMany(category => category.Values.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        var rows = categories.Select(category => $"""
            <tr><td>{E(category.Category)}</td><td>{category.Cases}</td>
            {string.Concat(columns.Select(name => $"<td>{Number(category.Values.GetValueOrDefault(name))}</td>"))}</tr>
            """);

        return Table("By category", $"Category;Cases;{string.Join(';', columns)}", rows);
    }

    /// <summary>Judge decisions with their reasons; omitted entirely when no judge ran.</summary>
    private static string Judges(EvaluationRunRecord run)
    {
        if (run.Judgements.Count == 0)
        {
            return string.Empty;
        }

        var rows = run.Judgements.Take(20).Select(judgement => $"""
            <tr><td>{E(judgement.CaseId)}</td><td>{E(judgement.Metric)}</td>
            <td>{(judgement.Value is null ? "n/a" : judgement.Value.Value.ToString("0.00"))}</td>
            <td>{E(judgement.Model)}</td><td>{E(judgement.Explanation)}</td></tr>
            """);

        return Table("Judge decisions", "Case;Metric;Score;Model;Explanation", rows);
    }

    private static string Failures(EvaluationRunRecord run)
    {
        var failures = run.Cases
            .Where(testCase => !testCase.Correct || testCase.Actual == CoreRentalNet.Evaluation.Datasets.Model.RagOutcome.Indeterminate)
            .Take(100)
            .Select(testCase => $"""
                <tr><td>{E(testCase.CaseId)}</td>
                <td>{E(testCase.Query)}</td>
                <td>{E(testCase.Expected.ToString())}</td><td>{E(testCase.Actual.ToString())}</td>
                <td>{E(testCase.FailedMetric ?? "-")}</td>
                <td>{E(string.Join(", ", testCase.FailureCodes))}</td>
                <td><code>{E(string.Join(", ", testCase.RetrievedSkus))}</code></td>
                <td><code>{E(string.Join(", ", testCase.RecommendedSkus))}</code></td></tr>
                """);

        return Table("Failure analysis", "Case;Query;Expected;Actual;Failed metric;Failures;Retrieved;Recommended", failures);
    }

    private static string Table(string caption, string header, IEnumerable<string> rows)
        => $"""
           <h2>{E(caption)}</h2>
           <table><thead><tr>{string.Concat(header.Split(';').Select(cell => $"<th>{E(cell)}</th>"))}</tr></thead>
           <tbody>{string.Concat(rows)}</tbody></table>
           """;

    private static string Status(EvaluationMetricResult metric) => metric.IsEvaluable ? "ok" : "weak";

    private static string Verdict(EvaluationMetricResult metric) => metric.Passed switch
    {
        true => "pass",
        false => "FAIL",
        null => "-",
    };

    private static string Interval(EvaluationMetricResult metric)
        => metric.Lower is null || metric.Upper is null
            ? "not available"
            : $"{metric.Lower:0.000} – {metric.Upper:0.000}";

    private static string Number(double? value) => value is null ? "not evaluable" : $"{value:0.000}";

    private static string Rate(int correct, int total)
        => total == 0 ? "n/a" : $"{correct / (double)total:0.000}";

    private static string Short(string hash) => hash.Length <= 12 ? hash : hash[..12];

    private static string E(string value) => WebUtility.HtmlEncode(value);

    private const string Style = """
        <style>
        body{font:14px/1.5 system-ui,sans-serif;margin:2rem;max-width:70rem;color:#111}
        h1{font-size:1.4rem;margin:0 0 .5rem} h2{font-size:1rem;margin:2rem 0 .5rem}
        .meta{color:#555;margin:.2rem 0} .lockbox{color:#b00}
        table{border-collapse:collapse;width:100%} th,td{border-bottom:1px solid #ddd;padding:.3rem .5rem;text-align:left}
        th{background:#f6f6f6;font-weight:600} tr.weak td{background:#fff5f5}
        code{font-family:ui-monospace,monospace;font-size:.85em}
        </style>
        """;
}
