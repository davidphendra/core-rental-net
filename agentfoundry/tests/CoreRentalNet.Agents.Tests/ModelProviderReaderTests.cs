using AwesomeAssertions;
using CoreRentalNet.Agents.Shared.Model;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

public sealed class ModelProviderReaderTests
{
    [Fact]
    public void An_absent_provider_keeps_the_foundry_default()
    {
        ModelProviderReader.Read(Configuration()).Should().Be(ModelProvider.Foundry);
    }

    [Fact]
    public void A_local_provider_is_read_without_regard_to_case()
    {
        var provider = ModelProviderReader.Read(
            Configuration((ModelProviderReader.Key, "openaicompatible")));

        provider.Should().Be(ModelProvider.OpenAiCompatible);
    }

    [Fact]
    public void An_unknown_provider_is_refused_by_name()
    {
        var act = () => ModelProviderReader.Read(
            Configuration((ModelProviderReader.Key, "llama")));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{ModelProviderReader.Key}*");
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(
                pair => pair.Key,
                pair => (string?)pair.Value))
            .Build();
}
