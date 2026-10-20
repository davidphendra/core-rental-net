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
    /// <c>agentfoundry/</c> and <c>evaluation/</c> are excluded on purpose. Both are independent solutions with
    /// their own pins and no central file: the hosted agent is built and released by Microsoft's tooling, and the
    /// evaluation harness pins a newer Foundry preview than the application uses.
    /// </remarks>
    private static IEnumerable<string> ProjectFiles()
        => Directory.GetFiles(RepoRoot.Path, "*.csproj", SearchOption.AllDirectories)
            .Where(file => !file.Contains("/obj/", StringComparison.Ordinal)
                           && !file.Contains("/bin/", StringComparison.Ordinal)
                           && !IsInIndependentSolution(file))
            .Concat([Path.Combine(RepoRoot.Path, "Directory.Packages.props")]);

    private static bool IsInIndependentSolution(string file)
    {
        var relative = Path.GetRelativePath(RepoRoot.Path, file);

        return relative.StartsWith("agentfoundry" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
               || relative.StartsWith("evaluation" + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }
}
