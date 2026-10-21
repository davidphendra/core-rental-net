using CoreRentalNet.Agents.Hosting;
using Microsoft.Extensions.Hosting;

namespace CoreRentalNet.Agents.Shared.Model;

/// <summary>Chooses the factory a deployment's configuration asks for.</summary>
/// <remarks>
/// The switch is at the composition boundary. A new provider adds a factory and one case here; the feature
/// registrations, stages and executors stay unchanged.
/// </remarks>
internal static class ModelClientFactorySelector
{
    /// <summary>The factory for the provider this host resolved.</summary>
    public static IChatClientFactory Select(
        AgentHostSettings settings,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        return settings.ModelProvider switch
        {
            ModelProvider.OpenAiCompatible => Local(configuration, settings.ModelTransport),
            ModelProvider.Foundry => new FoundryChatClientFactory(
                configuration,
                environment,
                settings.ModelTransport),
            _ => throw new ArgumentOutOfRangeException(
                nameof(settings),
                settings.ModelProvider,
                "Unknown model provider."),
        };
    }

    private static OpenAiCompatibleChatClientFactory Local(
        IConfiguration configuration,
        ModelTransportRetryOptions transport)
    {
        var endpoint = RequiredUri(configuration, OpenAiCompatibleChatClientFactory.EndpointKey);
        var model = Required(configuration, AgentFoundryRegistration.ModelKey);
        var apiKey = configuration[OpenAiCompatibleChatClientFactory.ApiKeyKey]?.Trim();

        return new OpenAiCompatibleChatClientFactory(
            endpoint,
            string.IsNullOrWhiteSpace(apiKey) ? "local" : apiKey,
            model,
            transport);
    }

    private static Uri RequiredUri(IConfiguration configuration, string key)
        => Uri.TryCreate(Required(configuration, key), UriKind.Absolute, out var endpoint)
            ? endpoint
            : throw new InvalidOperationException($"{key} must be an absolute URL.");

    private static string Required(IConfiguration configuration, string key)
        => configuration[key]?.Trim() is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"{key} is not configured.");
}
