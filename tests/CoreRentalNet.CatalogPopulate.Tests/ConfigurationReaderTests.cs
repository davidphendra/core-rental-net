using Xunit;
using AwesomeAssertions;
using CoreRentalNet.CatalogPopulate;
using Microsoft.Extensions.Configuration;

namespace CoreRentalNet.CatalogPopulate.Tests;

public class ConfigurationReaderTests
{
    [Fact]
    public void An_absent_model_takes_the_qwen_default()
    {
        var settings = ConfigurationReader.Read(Build(new Dictionary<string, string?>()), ".", Mode.Populate);

        settings.LlmModel.Should().Be("Qwen3-Coder-30B-A3B-Instruct");
    }

    [Fact]
    public void An_absent_source_path_refuses_the_run_by_name()
    {
        var configuration = Build(new Dictionary<string, string?> { ["Llm:Model"] = "local" });

        var act = () => ConfigurationReader.Read(configuration, ".", Mode.Extract);

        act.Should().Throw<ArgumentException>().WithMessage("*OfficeProducts*");
    }

    [Fact]
    public void The_endpoint_and_sample_size_take_their_defaults()
    {
        var settings = ConfigurationReader.Read(Complete(), "/repo", Mode.Populate);

        settings.LlmEndpoint.Should().Be("http://localhost:8080/v1");
        settings.ReviewSampleSize.Should().Be(20);
        settings.TargetTotal.Should().Be(400);
    }

    [Fact]
    public void A_relative_path_is_resolved_against_the_base_directory()
    {
        var settings = ConfigurationReader.Read(Complete(), "/repo", Mode.Populate);

        settings.CompactInputPath.Should().Be(Path.Combine("/repo", "App_Data/compact-catalogue.jsonl"));
    }

    private static IConfiguration Complete()
        => Build(new Dictionary<string, string?>
        {
            ["Llm:Model"] = "local-model",
            ["Sources:Meta:OfficeProducts"] = "m.jsonl",
            ["Sources:Meta:Electronics"] = "m.jsonl",
            ["Sources:Meta:ToolsAndHomeImprovement"] = "m.jsonl",
            ["Sources:Meta:HomeAndKitchen"] = "m.jsonl",
            ["Sources:Reviews:OfficeProducts"] = "r.jsonl",
            ["Sources:Reviews:Electronics"] = "r.jsonl",
            ["Sources:Reviews:ToolsAndHomeImprovement"] = "r.jsonl",
            ["Sources:Reviews:HomeAndKitchen"] = "r.jsonl",
        });

    private static IConfiguration Build(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}