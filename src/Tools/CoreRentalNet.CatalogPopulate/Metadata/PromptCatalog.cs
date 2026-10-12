using System.Reflection;

namespace CoreRentalNet.CatalogPopulate.Metadata;

/// <summary>The embedded instruction the model is asked with. The version is in the file name.</summary>
internal static class PromptCatalog
{
    private const string ResourceName = "CoreRentalNet.CatalogPopulate.Prompts.product-metadata.v1.md";

    private static readonly Lazy<string> s_text = new(Load);

    /// <summary>The prompt text.</summary>
    public static string Text => s_text.Value;

    private static string Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded prompt '{ResourceName}' is missing.");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
