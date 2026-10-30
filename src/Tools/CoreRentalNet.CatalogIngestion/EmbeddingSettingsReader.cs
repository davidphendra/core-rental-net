using System.Globalization;
using CoreRentalNet.BuildingBlocks.Application.Embeddings;
using Microsoft.Extensions.Configuration;

namespace CoreRentalNet.CatalogIngestion;

/// <summary>Reads the one embedding block both composition roots describe.</summary>
/// <remarks>
/// It is a separate reader because the embedding rules are not the chunker's or the paths': the provider
/// decides which schemes and keys are valid, and that decision is one cohesive unit.
/// </remarks>
internal static class EmbeddingSettingsReader
{
    /// <summary>The embedding settings this configuration describes.</summary>
    public static EmbeddingSettings Read(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = Provider(configuration);

        return new EmbeddingSettings(
            provider,
            Endpoint(configuration, provider),
            ApiKey(configuration, provider),
            Text(configuration, "Embedding:Model", IngestionSettings.DefaultEmbeddingModel),
            Positive(configuration, "Embedding:Width", IngestionSettings.DefaultWidth));
    }

    private static EmbeddingProvider Provider(IConfiguration configuration)
    {
        var stated = configuration["Embedding:Provider"]?.Trim();

        if (string.IsNullOrEmpty(stated))
        {
            return EmbeddingProvider.AzureOpenAI;
        }

        return Enum.TryParse<EmbeddingProvider>(stated, ignoreCase: true, out var provider)
            ? provider
            : throw new ArgumentException(
                $"Embedding:Provider must be {nameof(EmbeddingProvider.AzureOpenAI)}, "
                + $"{nameof(EmbeddingProvider.OpenAiCompatible)} or {nameof(EmbeddingProvider.Foundry)}, "
                + $"but was '{stated}'.");
    }

    private static Uri Endpoint(IConfiguration configuration, EmbeddingProvider provider)
    {
        var stated = Required(configuration, "Embedding:Endpoint");

        if (!Uri.TryCreate(stated, UriKind.Absolute, out var endpoint))
        {
            throw new ArgumentException(
                $"Embedding:Endpoint must be an absolute URL, but was '{stated}'.");
        }

        var accepted = provider == EmbeddingProvider.OpenAiCompatible
            ? endpoint.Scheme is "http" or "https"
            : endpoint.Scheme == "https";

        return accepted
            ? endpoint
            : throw new ArgumentException(
                $"Embedding:Endpoint is not valid for {provider}, but was '{stated}'.");
    }

    private static string? ApiKey(IConfiguration configuration, EmbeddingProvider provider)
        => provider switch
        {
            EmbeddingProvider.AzureOpenAI => Required(configuration, "Embedding:ApiKey"),
            EmbeddingProvider.OpenAiCompatible => configuration["Embedding:ApiKey"]?.Trim(),
            EmbeddingProvider.Foundry => null,
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unknown embedding provider."),
        };

    private static string Text(IConfiguration configuration, string key, string fallback)
    {
        var stated = configuration[key];

        if (stated is null)
        {
            return fallback;
        }

        return string.IsNullOrWhiteSpace(stated)
            ? throw new ArgumentException($"{key} was set to a blank value. Remove it to take the default, or give it a value.")
            : stated.Trim();
    }

    private static int Positive(IConfiguration configuration, string key, int fallback)
    {
        var stated = configuration[key];

        if (stated is null)
        {
            return fallback;
        }

        return int.TryParse(stated, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : throw new ArgumentException($"{key} must be a whole number of 1 or more, but was '{stated}'.");
    }

    private static string Required(IConfiguration configuration, string key)
        => configuration[key]?.Trim() is { Length: > 0 } value
            ? value
            : throw new ArgumentException($"{key} is not configured. Set it in appsettings.Local.json or the environment.");
}
