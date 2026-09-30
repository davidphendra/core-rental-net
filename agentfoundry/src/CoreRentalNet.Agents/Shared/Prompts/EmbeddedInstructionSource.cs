using System.Reflection;

namespace CoreRentalNet.Agents.Shared.Prompts;

/// <summary>Reads a prompt that is embedded into the assembly at build time.</summary>
/// <remarks>
/// <para>
/// Embedded rather than read from disk on purpose: a hosted agent is deployed as a zipped project folder,
/// and a prompt that did not travel with the binary would let the agent start and then fail to find its own
/// instructions.
/// </para>
/// <para>
/// The file name is a <b>constructor argument</b> rather than a constant, because there is one prompt per
/// agent and the roster is what decides which. A hard-coded name would mean the second agent could only be
/// added by editing this class, which is the opposite of what the roster is for.
/// </para>
/// <para>
/// The version is derived from the file name, so a change is a new file and a new version — never an edit
/// that keeps the old label.
/// </para>
/// </remarks>
internal sealed class EmbeddedInstructionSource(string fileName) : IInstructionSource
{
    private const string Prefix = "CoreRentalNet.Agents.Prompts.";

    public string Text { get; } = Read(Prefix + fileName);

    public string Version { get; } = Path.GetFileNameWithoutExtension(fileName);

    private static string Read(string resourceName)
    {
        using var stream = typeof(EmbeddedInstructionSource).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"The prompt '{resourceName}' is not embedded in the assembly. Prompts are embedded by the " +
                "CoreRentalNet.Agents.csproj EmbeddedResource item; if a file was renamed, the item and the " +
                "name passed here have to change together.");

        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
