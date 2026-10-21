using AwesomeAssertions;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion;
using CoreRentalNet.Agents.Hosting;
using CoreRentalNet.Agents.Shared.Model;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

public sealed class ModelClientFactorySelectorTests
{
    private const string AgentName = "core-rental-workspace-suggestion-agent";

    [Fact]
    public void The_foundry_provider_selects_the_foundry_factory()
    {
        var values = new[] { Workspace() };
        var settings = Settings(values);
        var factory = ModelClientFactorySelector.Select(
            settings,
            Configuration(values),
            new EnvironmentNamed("Development"));

        factory.Should().BeOfType<FoundryChatClientFactory>();
    }

    [Fact]
    public void The_local_provider_selects_the_openai_compatible_factory()
    {
        var values = new[]
        {
            Workspace(),
            (ModelProviderReader.Key, "OpenAiCompatible"),
            (OpenAiCompatibleChatClientFactory.EndpointKey, "http://localhost:8080/v1"),
            (AgentFoundryRegistration.ModelKey, "Mistral-Small-3.1-24B-Instruct-2503"),
        };

        var settings = Settings(values);
        var factory = ModelClientFactorySelector.Select(
            settings,
            Configuration(values),
            new EnvironmentNamed("Development"));

        factory.Should().BeOfType<OpenAiCompatibleChatClientFactory>();
    }

    [Fact]
    public void A_local_provider_without_an_endpoint_is_refused_by_name()
    {
        var values = new[]
        {
            Workspace(),
            (ModelProviderReader.Key, "OpenAiCompatible"),
            (AgentFoundryRegistration.ModelKey, "Mistral-Small-3.1-24B-Instruct-2503"),
        };

        var act = () => ModelClientFactorySelector.Select(
            Settings(values),
            Configuration(values),
            new EnvironmentNamed("Development"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{OpenAiCompatibleChatClientFactory.EndpointKey}*");
    }

    private static AgentHostSettings Settings(params (string Key, string Value)[] values)
        => AgentHostSettings.FromConfiguration(Configuration(values));

    private static (string Key, string Value) Workspace()
        => (WorkspaceSuggestionAgentIdentity.ConfigurationKey, AgentName);

    private static IConfiguration Configuration(params (string Key, string Value)[] values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(
                pair => pair.Key,
                pair => (string?)pair.Value))
            .Build();
}
