namespace CoreRentalNet.Agents.Shared.Model;

/// <summary>Reads which model protocol this deployment serves.</summary>
/// <remarks>
/// An absent setting keeps the Foundry path, so an existing deployment is unchanged. A value that is present
/// but unknown is refused rather than silently treated as Foundry: a typo must not send a local run to a cloud
/// project.
/// </remarks>
internal static class ModelProviderReader
{
    /// <summary>The configuration key, and the environment variable a local run sets.</summary>
    public const string Key = "MODEL_PROVIDER";

    /// <summary>The configured provider, or Foundry when nobody named one.</summary>
    public static ModelProvider Read(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var stated = configuration[Key]?.Trim();

        if (string.IsNullOrEmpty(stated))
        {
            return ModelProvider.Foundry;
        }

        return Enum.TryParse<ModelProvider>(stated, ignoreCase: true, out var provider)
            ? provider
            : throw new InvalidOperationException(
                $"{Key} must be {nameof(ModelProvider.Foundry)} or {nameof(ModelProvider.OpenAiCompatible)}, "
                + $"but was '{stated}'.");
    }
}
