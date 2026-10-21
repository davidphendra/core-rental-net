using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Application.Embeddings;
using CoreRentalNet.Host.Configs;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>How the Host decides whether its embedding capability is configured.</summary>
public sealed class VectorEmbeddingSettingsTests
{
    [Fact]
    public void A_local_provider_does_not_require_an_api_key()
    {
        var settings = Read(
            ("VectorEmbedding:Provider", "OpenAiCompatible"),
            ("VectorEmbedding:Endpoint", "http://localhost:8080/v1"),
            ("VectorEmbedding:Model", "all-MiniLM-L6-v2-embedding"));

        settings.Provider.Should().Be(EmbeddingProvider.OpenAiCompatible);
        settings.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public void The_azure_provider_still_requires_an_api_key()
    {
        var settings = Read(
            ("VectorEmbedding:Provider", "AzureOpenAI"),
            ("VectorEmbedding:Endpoint", "https://example.openai.azure.com/"),
            ("VectorEmbedding:Model", "text-embedding-3-small"));

        settings.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void An_absent_provider_keeps_the_azure_default()
    {
        var settings = Read(
            ("VectorEmbedding:Endpoint", "https://example.openai.azure.com/"),
            ("VectorEmbedding:ApiKey", "key"),
            ("VectorEmbedding:Model", "text-embedding-3-small"));

        settings.Provider.Should().Be(EmbeddingProvider.AzureOpenAI);
    }

    [Fact]
    public void An_unknown_provider_is_refused_by_name()
    {
        var act = () => Read(("VectorEmbedding:Provider", "Llama"));

        act.Should().Throw<InvalidOperationException>().WithMessage("*VectorEmbedding:Provider*");
    }

    [Fact]
    public void The_provider_neutral_value_carries_a_local_key_as_absent()
    {
        var settings = Read(
            ("VectorEmbedding:Provider", "OpenAiCompatible"),
            ("VectorEmbedding:Endpoint", "http://localhost:8080/v1"),
            ("VectorEmbedding:Model", "all-MiniLM-L6-v2-embedding"));

        var value = settings.ToEmbeddingSettings();

        value.Provider.Should().Be(EmbeddingProvider.OpenAiCompatible);
        value.ApiKey.Should().BeNull();
        value.Endpoint.Should().Be(new Uri("http://localhost:8080/v1"));
    }

    private static VectorEmbeddingSettings Read(params (string Key, string Value)[] values)
        => VectorEmbeddingSettings.From(
            new ConfigurationBuilder()
                .AddInMemoryCollection(values.ToDictionary(
                    pair => pair.Key,
                    pair => (string?)pair.Value))
                .Build());
}
