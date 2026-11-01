using System.Text.Json;
using AwesomeAssertions;
using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;
using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests.Runs;

/// <summary>A run record is written once, read back intact, and never silently overwritten.</summary>
public sealed class JsonEvaluationRunStoreTests
{
    [Fact]
    public async Task A_record_round_trips_with_its_provenance()
    {
        using var root = new TemporaryRoot();

        var path = await new JsonEvaluationRunStore(root.Path).SaveAsync(Record());

        path.Should().Be(System.IO.Path.Combine(root.Path, "run-1", "run.json"));
        var stored = JsonSerializer.Deserialize<EvaluationRunRecord>(
            await File.ReadAllTextAsync(path), EvaluationJson.Options)!;

        stored.DatasetSha256.Should().Be("c24238c9");
        stored.IsLockbox.Should().BeTrue();
        stored.GeneratorModel.Should().Be("Qwen3-Coder-30B-A3B-Instruct");
        stored.Metrics.Single().IsEvaluable.Should().BeTrue();
        stored.Cases.Single().Actual.Should().Be(RagOutcome.Indeterminate);
    }

    [Fact]
    public async Task Reusing_a_run_id_is_refused_rather_than_overwritten()
    {
        using var root = new TemporaryRoot();
        var store = new JsonEvaluationRunStore(root.Path);
        await store.SaveAsync(Record());

        var save = () => store.SaveAsync(Record());

        await save.Should().ThrowAsync<InvalidOperationException>();
    }

    private static EvaluationRunRecord Record() => new()
    {
        RunId = "run-1",
        Dataset = DatasetName.Test,
        DatasetVersion = "sha256:c24238c9786f;cases:100",
        DatasetSha256 = "c24238c9",
        IsLockbox = true,
        GeneratorModel = "Qwen3-Coder-30B-A3B-Instruct",
        JudgeModel = "gpt-oss-20b",
        Metrics = [new EvaluationMetricResult("outcome_accuracy", 0.5, 40, 0, 0.95, 0.3, 0.7, "evaluable")],
        Cases =
        [
            new CaseRunRecord("TST-001", "normal", RagOutcome.Success, RagOutcome.Indeterminate, false, [], ["WORKFLOW_STATUS"]),
        ],
    };
}

/// <summary>A directory that removes itself, so a run record never leaks into the repository.</summary>
internal sealed class TemporaryRoot : IDisposable
{
    public TemporaryRoot()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"rag-runs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
