using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Application.Embeddings;
using CoreRentalNet.BuildingBlocks.Infrastructure.Embeddings;
using Xunit;

namespace CoreRentalNet.BuildingBlocks.UnitTests.Embeddings;

/// <summary>The one place a composition root chooses how it embeds.</summary>
public sealed class EmbeddingServiceFactorySelectorTests
{
    [Fact]
    public void The_azure_provider_selects_the_azure_factory()
    {
        var factory = EmbeddingServiceFactorySelector.Select(EmbeddingProvider.AzureOpenAI);

        factory.Should().BeOfType<AzureOpenAiEmbeddingServiceFactory>();
    }

    [Fact]
    public void The_local_provider_selects_the_openai_compatible_factory()
    {
        var factory = EmbeddingServiceFactorySelector.Select(EmbeddingProvider.OpenAiCompatible);

        factory.Should().BeOfType<OpenAiCompatibleEmbeddingServiceFactory>();
    }

    [Fact]
    public void A_local_factory_builds_an_adapter_without_a_key()
    {
        var settings = new EmbeddingSettings(
            EmbeddingProvider.OpenAiCompatible,
            new Uri("http://localhost:8080/v1"),
            null,
            "all-MiniLM-L6-v2-embedding",
            384);

        var service = new OpenAiCompatibleEmbeddingServiceFactory().Create(settings);

        service.Should().BeOfType<OpenAiCompatibleEmbeddingService>();
    }

    [Fact]
    public void An_azure_factory_refuses_a_missing_key()
    {
        var settings = new EmbeddingSettings(
            EmbeddingProvider.AzureOpenAI,
            new Uri("https://example.openai.azure.com/"),
            null,
            "text-embedding-3-small",
            384);

        var act = () => new AzureOpenAiEmbeddingServiceFactory().Create(settings);

        act.Should().Throw<ArgumentException>();
    }
}
