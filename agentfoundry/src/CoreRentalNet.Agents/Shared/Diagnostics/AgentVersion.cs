using System.Reflection;
using System.Text.RegularExpressions;

namespace CoreRentalNet.Agents.Shared.Diagnostics;

/// <summary>The version the hosted agent was built as, and the commit it came from.</summary>
/// <remarks>
/// <para>
/// The agent is built remotely from a zipped project folder, so the repository root's version properties
/// do not reach it. The pipeline writes <c>Version.g.props</c> into the folder instead and this reads the
/// result. It is kept inside the project folder rather than linked to the application's reader, because a
/// linked file would not travel in the zip.
/// </para>
/// <para>
/// The rules are the application's: the version is the release without the commit, anything that is not a
/// semantic version is refused rather than shown, and the commit after "+" is build metadata and never
/// part of the version.
/// </para>
/// </remarks>
internal static partial class AgentVersion
{
    /// <summary>What a build with no stamp shows.</summary>
    public const string Fallback = "0.1.0";

    /// <summary>The running agent's version, without build metadata.</summary>
    public static string Current => Of(typeof(AgentVersion).Assembly);

    /// <summary>The commit the running agent was built from, or null when the build carried none.</summary>
    public static string? Build => BuildOf(typeof(AgentVersion).Assembly);

    /// <summary>The version an assembly was stamped with, or the floor when it cannot be read.</summary>
    public static string Of(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return Parse(assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
    }

    /// <summary>The commit an assembly was built from, or null when it carries none.</summary>
    public static string? BuildOf(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return ParseBuild(assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
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

    /// <summary>The commit in an informational version's build metadata, or null when there is none.</summary>
    public static string? ParseBuild(string? informationalVersion)
    {
        var plus = informationalVersion?.IndexOf('+', StringComparison.Ordinal) ?? -1;
        if (informationalVersion is null || plus < 0)
        {
            return null;
        }

        var metadata = informationalVersion[(plus + 1)..].Trim();
        return metadata.Length == 0 ? null : metadata;
    }

    [GeneratedRegex(@"^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$")]
    private static partial Regex SemanticVersion();
}
