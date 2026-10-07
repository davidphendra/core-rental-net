using System.Reflection;
using AwesomeAssertions;
using CoreRentalNet.Host.Presentation;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// Where the footer's version comes from.
/// </summary>
/// <remarks>
/// The host had no unit tests until this rule needed one. The browser suite starts the application
/// as a process and cannot see how it was stamped, and the version is stamped in
/// Directory.Build.props, so the rule that turns a stamp into something to display is checked here.
/// </remarks>
public sealed class AppVersionTests
{
    [Theory] // VER-01
    [InlineData("0.0.1", "0.0.1")]
    [InlineData("1.4.2", "1.4.2")]
    [InlineData("v1.4.2", "1.4.2")] // a pipeline passing the tag straight through
    [InlineData("1.4.2-rc.1", "1.4.2-rc.1")] // a pre-release label is part of a semantic version
    [InlineData("1.4.2+729b11c", "1.4.2")] // the commit is not part of the version
    [InlineData("1.4.2-rc.1+729b11c", "1.4.2-rc.1")]
    [InlineData("  1.4.2  ", "1.4.2")]
    [InlineData("", "0.1.0")]
    [InlineData("   ", "0.1.0")]
    [InlineData(null, "0.1.0")]
    [InlineData("release-7", "0.1.0")] // refused rather than shown
    [InlineData("1.4", "0.1.0")]
    [InlineData("1.4.2.0", "0.1.0")]
    public void A_version_is_shown_only_when_it_is_a_semantic_version(string? stamped, string shown)
        => AppVersion.Parse(stamped).Should().Be(shown);

    [Fact] // VER-02
    public void The_assembly_is_stamped_with_a_version_and_the_footer_shows_it()
    {
        var stamped = typeof(AppVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        stamped.Should().NotBeNull();

        // The footer is a version, not a fingerprint: whether or not the pipeline stamped a commit
        // after "+", what the footer shows is the semantic version alone, read from the assembly.
        AppVersion.Current.Should().MatchRegex(@"^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$");
        AppVersion.Current.Should().Be(AppVersion.Parse(stamped));
    }

    [Fact] // VER-04
    public void The_assembly_properties_follow_the_stabilized_mapping()
    {
        var assembly = typeof(AppVersion).Assembly;
        var release = Version.Parse(AppVersion.Current.Split('-')[0]);

        var assemblyVersion = assembly.GetName().Version!;
        assemblyVersion.Major.Should().Be(release.Major);
        assemblyVersion.Minor.Should().Be(release.Minor);
        assemblyVersion.Build.Should().Be(0, "the binding version is stabilized at MAJOR.MINOR.0.0");
        assemblyVersion.Revision.Should().Be(0);

        var fileVersion = Version.Parse(assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version);
        fileVersion.Major.Should().Be(release.Major);
        fileVersion.Minor.Should().Be(release.Minor);
        fileVersion.Build.Should().Be(release.Build);
        fileVersion.Revision.Should().Be(0, "the file version is the release MAJOR.MINOR.PATCH.0");
    }

    [Fact] // VER-06
    public void The_fallback_is_the_floor_the_build_declares()
    {
        // Two floors that drift apart is a silent lie: a refused stamp would show one number while an
        // untagged build stamps the other. The build declares its floor in Directory.Build.props.
        FloorFile().Should().Contain(
            $"<VersionPrefix>{AppVersion.Fallback}</VersionPrefix>",
            "the fallback the footer shows and the floor the build stamps have to be the same number");
    }

    [Fact] // VER-07
    public void A_metadata_value_is_read_by_key_and_an_absent_or_blank_one_is_nothing()
    {
        // Proven against this assembly rather than the host's, so the test does not depend on whether the
        // application it runs beside was stamped by the pipeline.
        var assembly = typeof(AppVersionTests).Assembly;

        AppVersion.MetadataOf(assembly, "BuildIdentityTestKey").Should().Be("stamped-value");
        AppVersion.MetadataOf(assembly, "BuildIdentityTestBlankKey").Should().BeNull();
        AppVersion.MetadataOf(assembly, "BuildIdentityTestMissingKey").Should().BeNull();
    }

    /// <summary>The first Directory.Build.props above the test output, which is the repository's own.</summary>
    private static string FloorFile()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "Directory.Build.props");

            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate Directory.Build.props above the test output.");
    }
}
