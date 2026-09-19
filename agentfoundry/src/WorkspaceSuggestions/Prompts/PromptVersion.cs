namespace WorkspaceSuggestions.Prompts;

/// <summary>The version of a prompt, taken from the file it was loaded from.</summary>
/// <remarks>
/// The version is in the file name — <c>suggestor.v1.md</c> — so it is derived rather than written down a
/// second time, where the two copies would eventually disagree. A run reports the version that actually
/// answered, which is only true if nothing can be forgotten when a prompt is revised: renaming the file to
/// <c>suggestor.v2.md</c> changes what the run reports, with no second place to remember.
/// </remarks>
internal static class PromptVersion
{
    public const string Extension = ".md";

    public static string From(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        return fileName.EndsWith(Extension, StringComparison.OrdinalIgnoreCase)
            ? fileName[..^Extension.Length]
            : fileName;
    }
}
