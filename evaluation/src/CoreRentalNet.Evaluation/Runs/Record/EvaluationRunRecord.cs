using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;

using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;
namespace CoreRentalNet.Evaluation.Runs.Record;

/// <summary>Everything needed to decide whether two runs may be compared at all.</summary>
/// <remarks>
/// The dataset hash and the two model identifiers are stored beside the scores. Without them a report cannot
/// distinguish "the application improved" from "a different dataset or a different judge produced the number".
/// </remarks>
public sealed record EvaluationRunRecord
{
    public required string RunId { get; init; }

    public required DatasetName Dataset { get; init; }

    public required string DatasetVersion { get; init; }

    public required string DatasetSha256 { get; init; }

    public bool IsLockbox { get; init; }

    public string AppVersion { get; init; } = string.Empty;

    public string GeneratorModel { get; init; } = string.Empty;

    public string JudgeModel { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> RetrievalConfiguration { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    public DateTimeOffset StartedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public double DurationMs { get; init; }

    public int TotalCases { get; init; }

    public int Correct { get; init; }

    public int Incorrect { get; init; }

    public int Indeterminate { get; init; }

    public IReadOnlyList<EvaluationMetricResult> Metrics { get; init; } = [];

    public IReadOnlyList<CaseRunRecord> Cases { get; init; } = [];

    /// <summary>Judge decisions with their explanations; empty when no judge ran.</summary>
    public IReadOnlyList<CaseJudgement> Judgements { get; init; } = [];

    public IReadOnlyList<DatasetExclusion> Exclusions { get; init; } = [];
}
