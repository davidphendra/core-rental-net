using System.Security.Cryptography;
using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;
using Xunit;
using AwesomeAssertions;

namespace CoreRentalNet.Evaluation.Tests.Datasets;

/// <summary>A dataset is read in place, and its hash covers the exact bytes on disk.</summary>
public sealed class DatasetLoaderTests
{
    [Fact]
    public async Task The_hash_is_the_sha256_of_the_file_it_read()
    {
        var path = await TemporaryDataset.WriteAsync(Json);

        try
        {
            var before = await File.ReadAllBytesAsync(path);
            var dataset = await new DatasetLoader().LoadAsync(DatasetName.Validation, path);

            dataset.Sha256.Should().Be(Convert.ToHexStringLower(SHA256.HashData(before)));
            dataset.SizeInBytes.Should().Be(before.LongLength);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task The_file_is_unchanged_by_a_load()
    {
        var path = await TemporaryDataset.WriteAsync(Json);

        try
        {
            var before = await File.ReadAllBytesAsync(path);
            await new DatasetLoader().LoadAsync(DatasetName.Validation, path);
            (await File.ReadAllBytesAsync(path)).Should().Equal(before);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Cases_keep_their_annotations()
    {
        var path = await TemporaryDataset.WriteAsync(Json);

        try
        {
            var dataset = await new DatasetLoader().LoadAsync(DatasetName.Validation, path);
            var testCase = dataset.Cases.Single();

            testCase.Id.Should().Be("VAL-001");
            testCase.ExpectedOutcome.Should().Be(RagOutcome.Success);
            testCase.ExpectedEquipment.Monitors.Should().Be(2);
            testCase.RelevantWorkspaceIds.Should().Equal("MONB086Q776F");
            testCase.Facts.Source.Should().Be(ExpectedFactsSource.Derived);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task A_malformed_file_throws_rather_than_loading_nothing()
    {
        var path = await TemporaryDataset.WriteAsync("""{ "not": "an array" }""");

        try
        {
            var load = () => new DatasetLoader().LoadAsync(DatasetName.Validation, path);
            await load.Should().ThrowAsync<System.Text.Json.JsonException>();
        }
        finally
        {
            File.Delete(path);
        }
    }

    private const string Json = """
    [
      {
        "id": "VAL-001",
        "category": "normal",
        "query": "I need one desk, one chair, and two monitors.",
        "expectedOutcome": "success",
        "expectedEquipment": { "desks": 1, "chairs": 1, "monitors": 2, "lamps": 0, "plants": 0, "beanBags": 0, "coffeeMachines": 0 },
        "expectedWorkspaceIds": ["MONB086Q776F"],
        "relevantWorkspaceIds": ["MONB086Q776F"]
      }
    ]
    """;
}

/// <summary>Writes a dataset to a uniquely named temporary file.</summary>
internal static class TemporaryDataset
{
    public static async Task<string> WriteAsync(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"rag-dataset-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, json);
        return path;
    }
}
