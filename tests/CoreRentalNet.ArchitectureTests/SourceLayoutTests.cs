using AwesomeAssertions;
using Xunit;

namespace CoreRentalNet.ArchitectureTests;

/// <summary>
/// Source-level rules. NetArchTest works on compiled types and cannot see a table name or a
/// DateTime call, so these few rules read the source instead. Weak but deterministic.
/// </summary>
public sealed class SourceLayoutTests
{
    private static readonly string[] ModuleNames = ["Catalog", "Workspace", "Rentals"];

    [Fact] // ARC-02
    public void Each_persisting_module_declares_at_most_one_DbContext()
    {
        foreach (var module in ExistingModules())
        {
            var declarations = ModuleSourceFiles(module)
                .SelectMany(file => File.ReadAllLines(file).Select(line => (File: file, Line: line)))
                .Where(entry => entry.Line.Contains(": DbContext", StringComparison.Ordinal)
                                || entry.Line.Contains(": ModuleDbContext", StringComparison.Ordinal))
                .ToArray();

            declarations.Should().HaveCountLessThanOrEqualTo(
                1,
                $"module {module} must own at most one DbContext, but found: {string.Join(", ", declarations.Select(d => d.File))}");
        }
    }

    [Fact] // ARC-03
    public void Every_mapped_table_name_carries_its_module_prefix()
    {
        foreach (var module in ExistingModules())
        {
            var usages = ModuleSourceFiles(module)
                .SelectMany(file => File.ReadAllLines(file))
                .Where(line => line.Contains(".ToTable(", StringComparison.Ordinal))
                .ToArray();

            foreach (var usage in usages)
            {
                var start = usage.IndexOf(".ToTable(\"", StringComparison.Ordinal) + ".ToTable(\"".Length;
                var end = usage.IndexOf('"', start);
                var tableName = usage[start..end];

                tableName.Should().StartWith(
                    $"{module}_",
                    $"table '{tableName}' must be prefixed with its owning module (ADR-0002)");
            }
        }
    }

    [Fact] // ARC-06
    public void No_domain_source_reads_the_clock_directly()
    {
        var offenders = new List<string>();

        foreach (var module in ExistingModules())
        {
            var directory = RepoRoot.Combine("src", "Modules", module);
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var project in Directory.GetDirectories(directory, "*.Domain"))
            {
                foreach (var file in Directory.GetFiles(project, "*.cs", SearchOption.AllDirectories))
                {
                    if (file.Contains("/obj/", StringComparison.Ordinal) || file.Contains("/bin/", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var text = File.ReadAllText(file);

                    if (text.Contains("DateTime.Now", StringComparison.Ordinal)
                        || text.Contains("DateTime.UtcNow", StringComparison.Ordinal)
                        || text.Contains("DateTimeOffset.Now", StringComparison.Ordinal)
                        || text.Contains("DateTimeOffset.UtcNow", StringComparison.Ordinal))
                    {
                        offenders.Add(file);
                    }
                }
            }
        }

        offenders.Should().BeEmpty(
            "Domain code must take the time from an injected TimeProvider so tests can control it (ADR-0011)");
    }

    private static IEnumerable<string> ExistingModules()
        => ModuleNames.Where(module => Directory.Exists(RepoRoot.Combine("src", "Modules", module)));

    private static IEnumerable<string> ModuleSourceFiles(string module)
    {
        var directory = RepoRoot.Combine("src", "Modules", module);

        return Directory
            .GetFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains("/obj/", StringComparison.Ordinal)
                           && !file.Contains("/bin/", StringComparison.Ordinal));
    }
}
