using AwesomeAssertions;
using CoreRentalNet.CatalogIngestion.Chunking;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CoreRentalNet.CatalogIngestion.Tests;

/// <summary>
/// What the tool accepts from its settings file.
/// </summary>
/// <remarks>
/// The rule under test has two halves, and the second is the one that matters: an <b>absent</b> key takes
/// its default, while a key that is <b>present but unusable</b> stops the run. A silently substituted value
/// would leave no trace, because nothing else records what a run actually used.
/// </remarks>
public sealed class ConfigurationReaderTests
{
    /// <summary>A complete configuration. Each test removes or damages exactly one value.</summary>
    private static Dictionary<string, string?> EveryKey() => new()
    {
        ["Catalog:FilePath"] = "/tmp/products.json",
        ["Database:Path"] = "/tmp/product_embedding.db",
        ["Embedding:Endpoint"] = "https://example.openai.azure.com/",
        ["Embedding:ApiKey"] = "key",
        ["Embedding:Model"] = "text-embedding-3-small",
        ["Embedding:Width"] = "384",
        ["Chunker:TokenLimit"] = "256",
        ["Chunker:BufferSize"] = "1",
        ["Chunker:ThresholdType"] = "Percentile",
        ["Chunker:ThresholdAmount"] = "95",
        ["Chunker:MinChunkChars"] = "1",
        ["Chunker:MaxOverrunChars"] = "200",
    };

    private const string BaseDirectory = "/base";

    private static IngestionSettings Read(IDictionary<string, string?> values)
        => ConfigurationReader.Read(new ConfigurationBuilder().AddInMemoryCollection(values).Build(), BaseDirectory);

    [Fact]
    public void Every_value_is_read_from_configuration()
    {
        var settings = Read(EveryKey());

        settings.CataloguePath.Should().Be("/tmp/products.json");
        settings.DatabasePath.Should().Be("/tmp/product_embedding.db");
        settings.EmbeddingEndpoint.Should().Be("https://example.openai.azure.com/");
        settings.EmbeddingApiKey.Should().Be("key");
        settings.EmbeddingModel.Should().Be("text-embedding-3-small");
        settings.Width.Should().Be(384);
        settings.Chunker.Should().Be(new ChunkerSettings(256, 1, "Percentile", 95, null, 1, 200));
    }

    [Fact]
    public void An_absent_key_takes_its_default_rather_than_stopping_the_run()
    {
        // The endpoint and the key are the two values a run cannot invent; every other key has a default, so a
        // file only has to say what differs from them.
        var values = new Dictionary<string, string?>
        {
            ["Embedding:Endpoint"] = "https://example.openai.azure.com/",
            ["Embedding:ApiKey"] = "key",
        };

        var settings = Read(values);

        settings.EmbeddingModel.Should().Be(IngestionSettings.DefaultEmbeddingModel);
        settings.Width.Should().Be(IngestionSettings.DefaultWidth);
        settings.Chunker.TokenLimit.Should().Be(ChunkerSettings.DefaultTokenLimit);
        settings.Chunker.BufferSize.Should().Be(ChunkerSettings.DefaultBufferSize);
        settings.Chunker.ThresholdType.Should().Be(ChunkerSettings.DefaultThresholdType);
        settings.Chunker.ThresholdAmount.Should().Be(ChunkerSettings.DefaultThresholdAmount);
        settings.Chunker.MinChunkChars.Should().Be(ChunkerSettings.DefaultMinChunkChars);
        settings.Chunker.MaxOverrunChars.Should().Be(ChunkerSettings.DefaultMaxOverrunChars);
    }

    [Theory]
    [InlineData("Embedding:Width", "abc")]
    [InlineData("Embedding:Width", "0")]
    [InlineData("Embedding:Width", "")]
    [InlineData("Embedding:Endpoint", "not-a-url")]
    [InlineData("Embedding:Endpoint", "/v1")]
    [InlineData("Embedding:Endpoint", "ftp://localhost")]
    [InlineData("Embedding:Endpoint", "http://localhost")]
    [InlineData("Embedding:Endpoint", "")]
    [InlineData("Embedding:ApiKey", "")]
    [InlineData("Embedding:Model", "")]
    [InlineData("Chunker:ThresholdType", "Vibes")]
    [InlineData("Chunker:TokenLimit", "-1")]
    [InlineData("Chunker:ThresholdAmount", "zero")]
    [InlineData("Chunker:BufferSize", "many")]
    public void A_value_that_is_present_but_unusable_is_refused_and_the_key_is_named(string key, string value)
    {
        // The distinction is the point: an omission is "use what you would", a typo is an attempt the tool
        // must not quietly replace. The message names the key so the fix is one edit away.
        var values = EveryKey();
        values[key] = value;

        var act = () => Read(values);

        act.Should().Throw<ArgumentException>().WithMessage($"*{key}*");
    }

    [Fact]
    public void The_threshold_type_is_read_without_regard_to_case()
    {
        var values = EveryKey();
        values["Chunker:ThresholdType"] = "interquartile";

        Read(values).Chunker.ThresholdType.Should().Be("InterQuartile");
    }

    [Fact]
    public void The_target_chunk_count_is_the_one_value_whose_absence_is_meaningful()
    {
        // Unset means the thresholds decide; set means they are ignored. That is two modes, not a default.
        Read(EveryKey()).Chunker.TargetChunkCount.Should().BeNull();

        var withCount = EveryKey();
        withCount["Chunker:TargetChunkCount"] = "3";

        Read(withCount).Chunker.TargetChunkCount.Should().Be(3);
    }

    [Fact]
    public void A_relative_path_resolves_against_the_project_directory()
    {
        var values = EveryKey();
        values["Catalog:FilePath"] = "products.json";
        values["Database:Path"] = "App_Data/product_embedding.db";

        var settings = Read(values);

        Path.IsPathRooted(settings.CataloguePath).Should().BeTrue();
        Path.IsPathRooted(settings.DatabasePath).Should().BeTrue();
        settings.CataloguePath.Should().EndWith("products.json");
        settings.DatabasePath.Should().EndWith(Path.Combine("App_Data", "product_embedding.db"));
    }

    [Fact]
    public void The_usage_names_every_key_the_reader_understands()
    {
        // The help text and the reader are two statements about the same file, so they are held against each
        // other: a key added to one and not the other fails here rather than surprising an operator who read
        // the help.
        foreach (var key in EveryKey().Keys.Append("Chunker:TargetChunkCount"))
        {
            Usage.Text.Should().Contain(key);
        }
    }
}
