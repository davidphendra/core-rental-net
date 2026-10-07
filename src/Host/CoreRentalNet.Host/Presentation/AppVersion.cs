using System.Reflection;
using System.Text.RegularExpressions;

namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// The build identity the footer shows and the telemetry resource carries.
/// </summary>
/// <remarks>
/// <para>
/// The value is declared once, in Directory.Build.props, and a release pipeline overrides it with
/// the tag it is building: <c>dotnet build -p:Version=${TAG#v}</c>. The build never reads git, which
/// is the practice in the .NET platform repositories - dotnet/runtime, aspnetcore, roslyn and efcore
/// each declare <c>VersionPrefix</c> and let the release step pass the version it is building - and
/// it means an untagged build shows a floor rather than a guess.
/// </para>
/// <para>
/// The version is read from the artifact's own <c>AssemblyInformationalVersionAttribute</c>. The
/// pipeline may stamp a commit after "+" (<c>1.4.1+abc1234</c>) as build metadata; the footer is a
/// version, so that is trimmed away and what remains is validated as a semantic version.
/// </para>
/// <para>
/// <b>The other three fields are assembly metadata rather than a file beside the assembly.</b> They
/// say which commit the artifact was built from, which pipeline run produced it, and where it was
/// built to run. Being in the binary is the point: no deployment can change them without rebuilding,
/// and a build that carried none reports nothing rather than an empty identity.
/// </para>
/// </remarks>
public static partial class AppVersion
{
    /// <summary>What an unreleased build shows.</summary>
    public const string Fallback = "0.1.0";

    /// <summary>The running application's version.</summary>
    public static string Current => Of(typeof(AppVersion).Assembly);

    /// <summary>The commit the artifact was built from, or null when the build carried none.</summary>
    public static string? GitSha => MetadataOf(typeof(AppVersion).Assembly, GitShaKey);

    /// <summary>The pipeline run that produced the artifact, or null when the build carried none.</summary>
    public static string? BuildId => MetadataOf(typeof(AppVersion).Assembly, BuildIdKey);

    /// <summary>Where the artifact was built to run, or null when the build named no environment.</summary>
    public static string? Environment => MetadataOf(typeof(AppVersion).Assembly, EnvironmentKey);

    /// <summary>The version an assembly was stamped with, or the floor when it cannot be read.</summary>
    public static string Of(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return Parse(assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
    }

    /// <summary>A metadata value an assembly was stamped with, or null when it carries none.</summary>
    /// <remarks>An empty value is not an identity, so a stamped-but-blank value is refused rather than read.</remarks>
    public static string? MetadataOf(Assembly assembly, string key)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var value = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => string.Equals(attribute.Key, key, StringComparison.Ordinal))
            ?.Value;

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// The semantic version in an informational version, or <see cref="Fallback"/>.
    /// </summary>
    /// <remarks>
    /// Anything that is not a semantic version is refused rather than displayed: a pipeline passing
    /// a tag such as "release-7" should leave the footer reading the floor, not a string that claims
    /// to be a version and is not one.
    /// </remarks>
    public static string Parse(string? informationalVersion)
    {
        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            return Fallback;
        }

        // Build metadata after "+" is dropped: it is a fingerprint, not part of the version.
        var plus = informationalVersion.IndexOf('+', StringComparison.Ordinal);
        var withoutMetadata = (plus < 0 ? informationalVersion : informationalVersion[..plus]).Trim();

        // Tags are conventionally v-prefixed, and a pipeline may well pass one straight through.
        var withoutTagPrefix = withoutMetadata.StartsWith('v') ? withoutMetadata[1..] : withoutMetadata;

        return SemanticVersion().IsMatch(withoutTagPrefix) ? withoutTagPrefix : Fallback;
    }

    /// <summary>The assembly metadata keys the pipeline stamps the build identity under.</summary>
    private const string GitShaKey = "GitSha";
    private const string BuildIdKey = "BuildId";
    private const string EnvironmentKey = "Environment";

    [GeneratedRegex(@"^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$")]
    private static partial Regex SemanticVersion();
}
