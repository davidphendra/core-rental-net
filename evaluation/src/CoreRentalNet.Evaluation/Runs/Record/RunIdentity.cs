using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;

namespace CoreRentalNet.Evaluation.Runs.Record;

/// <summary>What a run record must carry about the thing that produced it, independent of the scores.</summary>
public sealed record RunIdentity(
    string RunId,
    string AppVersion,
    string GeneratorModel,
    string JudgeModel,
    DateTimeOffset StartedAtUtc,
    double DurationMs)
{
    public IReadOnlyDictionary<string, string> RetrievalConfiguration { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);
}
