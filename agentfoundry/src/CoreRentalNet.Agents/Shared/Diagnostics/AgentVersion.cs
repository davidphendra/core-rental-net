using System.Reflection;
using System.Text.RegularExpressions;

namespace CoreRentalNet.Agents.Shared.Diagnostics;

/// <summary>The build identity the hosted agent was built with, read from its own metadata.</summary>
/// <remarks>
/// <para>
/// The agent is built remotely from a zipped project folder, so the repository root's version properties do
/// not reach it. The pipeline writes <c>Version.g.props</c> into the folder instead and this reads the
/// result. It is kept inside the project folder rather than linked to the application's reader, because a
/// linked file would not travel in the zip.
/// </para>
/// <para>
/// The identity is baked into the artifact as assembly metadata rather than read from the process
/// environment, so the running agent reports the build it was made from and nothing a process can set.
/// </para>
/// </remarks>
internal static partial class AgentVersion
{
    /// <summary>What a build with no stamp shows.</summary>
    public const string Fallback = "0.1.0";

    /// <summary>The running agent's version, without build metadata.</summary>
    public static string Current => Of(typeof(AgentVersion).Assembly);

    /// <summary>The commit the running agent was built from, or null when the build carried none.</summary>
    public static string? GitSha => MetadataOf(typeof(AgentVersion).Assembly, GitShaKey);

    /// <summary>The pipeline run that produced the running agent, or null when the build carried none.</summary>
    public static string? BuildId => MetadataOf(typeof(AgentVersion).Assembly, BuildIdKey);

    /// <summary>Where the running agent was built to run, or null when the build named no environment.</summary>
    public static string? Environment => MetadataOf(typeof(AgentVersion).Assembly, EnvironmentKey);

    /// <summary>The version an assembly was stamped with, or the floor when it cannot be read.</summary>
    public static string Of(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return Parse(assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
    }

    /// <summary>The semantic version in an informational version, or <see cref="Fallback"/>.</summary>
    public static string Parse(string? informationalVersion)
    {
        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            return Fallback;
        }

        var plus = informationalVersion.IndexOf('+', StringComparison.Ordinal);
        var withoutMetadata = (plus < 0 ? informationalVersion : informationalVersion[..plus]).Trim();
        var withoutTagPrefix = withoutMetadata.StartsWith('v') ? withoutMetadata[1..] : withoutMetadata;

        return SemanticVersion().IsMatch(withoutTagPrefix) ? withoutTagPrefix : Fallback;
    }

    /// <summary>A metadata value an assembly was stamped with, or null when it carries none.</summary>
    /// <remarks>An empty value is not an identity, so a stamped-but-blank value is refused rather than shown.</remarks>
    public static string? MetadataOf(Assembly assembly, string key)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var value = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => string.Equals(attribute.Key, key, StringComparison.Ordinal))
            ?.Value;

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>The assembly metadata keys the pipeline stamps the build identity under.</summary>
    private const string GitShaKey = "GitSha";
    private const string BuildIdKey = "BuildId";
    private const string EnvironmentKey = "Environment";

    [GeneratedRegex(@"^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$")]
    private static partial Regex SemanticVersion();
}
