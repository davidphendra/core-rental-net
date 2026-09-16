using System.Text.RegularExpressions;
using AwesomeAssertions;
using Xunit;

namespace CoreRentalNet.ArchitectureTests;

/// <summary>
/// One type per file, named after the type.
/// </summary>
/// <remarks>
/// Source-level, because NetArchTest works on compiled types and cannot see how they are arranged in
/// files. A file of top-level statements (the two <c>Program.cs</c> entry points) declares no type and
/// is skipped; generated migrations are ignored because their file names carry a timestamp.
/// </remarks>
public sealed partial class OneTypePerFileTests
{
    [Fact] //
    public void Every_source_file_declares_at_most_one_type_named_after_the_file()
    {
        var offenders = new List<string>();
        var filesWithAType = 0;

        foreach (var file in SourceFiles())
        {
            var names = TypeDeclaration()
                .Matches(File.ReadAllText(file))
                .Select(match => match.Groups[1].Value)
                .ToArray();

            if (names.Length == 0)
            {
                continue;
            }

            filesWithAType++;

            if (names.Length > 1)
            {
                offenders.Add($"{file}: {names.Length} types ({string.Join(", ", names)})");
            }
            else if (!string.Equals(names[0], Path.GetFileNameWithoutExtension(file), StringComparison.Ordinal))
            {
                offenders.Add($"{file}: type '{names[0]}' is not the file name");
            }
        }

        // Guards against the rule passing because it found nothing to check.
        filesWithAType.Should().BeGreaterThan(100, "the rule must be checked against real files");

        offenders.Should().BeEmpty("one type per file, named after the type");
    }

    private static IEnumerable<string> SourceFiles()
        => new[] { "src", "tests" }
            .Select(root => RepoRoot.Combine(root))
            .Where(Directory.Exists)
            .SelectMany(root => Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(file => !file.Contains("/obj/", StringComparison.Ordinal)
                           && !file.Contains("/bin/", StringComparison.Ordinal)
                           && !file.Contains("/Migrations/", StringComparison.Ordinal));

    [GeneratedRegex(
        @"^(?:public|internal|private|protected)?\s*(?:sealed\s+|abstract\s+|static\s+|partial\s+|readonly\s+)*(?:record\s+struct|record|class|interface|enum|struct)\s+([A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.Multiline)]
    private static partial Regex TypeDeclaration();
}
