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

/// <summary>Baseline resolution is proven without executing an agent.</summary>
public sealed class RunBaselineLocatorTests
{
    [Fact]
    public void Previous_selects_the_latest_earlier_run_of_the_same_dataset()
    {
        using var root = new BaselineRoot();
        root.Write("dev-01", DatasetName.Development, "2026-11-01T09:00:00Z");
        root.Write("dev-02", DatasetName.Development, "2026-11-01T11:00:00Z");
        root.Write("val-01", DatasetName.Validation, "2026-11-01T12:00:00Z");
        root.Write("dev-03", DatasetName.Development, "2026-11-01T13:00:00Z");

        var found = root.Locator.Resolve("previous", DatasetName.Development, "dev-03");

        found!.RunId.Should().Be("dev-02");
    }

    [Fact]
    public void A_run_id_resolves_to_its_run_json()
    {
        using var root = new BaselineRoot();
        root.Write("dev-01", DatasetName.Development, "2026-11-01T09:00:00Z");

        root.Locator.Resolve("dev-01", DatasetName.Development, "dev-02")!.RunId.Should().Be("dev-01");
    }

    [Fact]
    public void A_path_is_used_as_given()
    {
        using var root = new BaselineRoot();
        var path = root.Write("dev-01", DatasetName.Development, "2026-11-01T09:00:00Z");

        root.Locator.Resolve(path, DatasetName.Development, "dev-02")!.RunId.Should().Be("dev-01");
    }

    [Fact]
    public void An_unknown_target_is_null_rather_than_an_exception()
        => new RunBaselineLocator(Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}"))
            .Resolve("nope", DatasetName.Validation, "run-1").Should().BeNull();

    private sealed class BaselineRoot : IDisposable
    {
        public BaselineRoot()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"baselines-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public RunBaselineLocator Locator => new(Path);

        public string Write(string runId, DatasetName dataset, string startedAt)
        {
            var directory = System.IO.Path.Combine(Path, runId);
            Directory.CreateDirectory(directory);
            var file = System.IO.Path.Combine(directory, "run.json");
            File.WriteAllText(file, $$"""
                {"runId":"{{runId}}","dataset":"{{dataset}}","datasetVersion":"v1",
                 "datasetSha256":"hash","startedAtUtc":"{{startedAt}}","metrics":[],"cases":[]}
                """);
            return file;
        }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
