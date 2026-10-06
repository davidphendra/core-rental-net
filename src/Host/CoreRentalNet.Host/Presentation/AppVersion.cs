using System.Reflection;
using System.Text.RegularExpressions;

namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// The version the footer shows.
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
/// </remarks>
public static partial class AppVersion
{
    /// <summary>What an unreleased build shows.</summary>
    public const string Fallback = "0.1.0";

    /// <summary>The running application's version.</summary>
    public static string Current => Of(typeof(AppVersion).Assembly);

    /// <summary>The version an assembly was stamped with, or the floor when it cannot be read.</summary>
    public static string Of(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return Parse(assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
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

    [GeneratedRegex(@"^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$")]
    private static partial Regex SemanticVersion();
}
