using AwesomeAssertions;
using Xunit;

namespace CoreRentalNet.ArchitectureTests;

public sealed class PackagePolicyTests
{
    private static readonly string[] BannedPackages =
    [
        "MediatR",
        "MassTransit",
        "NServiceBus",
        "Rebus",
        "Wolverine",
        "Marten",
        "AutoMapper",
        "Moq",
        "NSubstitute",
        "Microsoft.EntityFrameworkCore.InMemory",
        "Testcontainers",
    ];

    [Fact] // ARC-05
    public void No_deliberately_excluded_package_is_referenced()
    {
        var offenders = ProjectFiles()
            .SelectMany(file => File.ReadAllLines(file).Select(line => (File: file, Line: line)))
            .Where(entry => BannedPackages.Any(banned =>
                entry.Line.Contains($"Include=\"{banned}\"", StringComparison.Ordinal)))
            .Select(entry => $"{entry.File}: {entry.Line.Trim()}")
            .ToArray();

        offenders.Should().BeEmpty(" excludes these packages and excludes a mediator");
    }

    [Fact] // ARC-05
    public void Every_package_version_is_pinned_exactly()
    {
        var central = Path.Combine(RepoRoot.Path, "Directory.Packages.props");
        File.Exists(central).Should().BeTrue();

        var floating = File.ReadAllLines(central)
            .Where(line => line.Contains("Version=\"", StringComparison.Ordinal))
            .Where(line => line.Contains('*') || line.Contains("latest", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        floating.Should().BeEmpty("a floating version is how a vulnerable transitive reappears");
    }

    [Fact] // ARC-05
    public void Projects_do_not_pin_versions_themselves()
    {
        var offenders = ProjectFiles()
            .SelectMany(file => File.ReadAllLines(file).Select(line => (File: file, Line: line)))
            .Where(entry => entry.Line.Contains("<PackageReference", StringComparison.Ordinal)
                            && entry.Line.Contains("Version=", StringComparison.Ordinal))
            .Select(entry => $"{entry.File}: {entry.Line.Trim()}")
            .ToArray();

        offenders.Should().BeEmpty("package versions live in Directory.Packages.props only");
    }

    /// <summary>
    /// The application's projects, and its one central file.
    /// </summary>
    /// <remarks>
    /// <c>agentfoundry/</c> is excluded on purpose. It is a second, independent solution (ADR 0003) with its
    /// own pins and no central file: the hosted agent is built and released by Microsoft's tooling on Microsoft's
    /// cadence, and it deploys separately from this application, so this application's policy is not its policy.
    /// </remarks>
    private static IEnumerable<string> ProjectFiles()
        => Directory.GetFiles(RepoRoot.Path, "*.csproj", SearchOption.AllDirectories)
            .Where(file => !file.Contains("/obj/", StringComparison.Ordinal)
                           && !file.Contains("/bin/", StringComparison.Ordinal)
                           && !IsInAgentTree(file))
            .Concat([Path.Combine(RepoRoot.Path, "Directory.Packages.props")]);

    private static bool IsInAgentTree(string file)
        => Path.GetRelativePath(RepoRoot.Path, file)
            .StartsWith("agentfoundry" + Path.DirectorySeparatorChar, StringComparison.Ordinal);
}
