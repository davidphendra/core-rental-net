using AwesomeAssertions;
using Xunit;

namespace CoreRentalNet.ArchitectureTests;

/// <summary>
/// The coding budgets from : a file holds at most 150 lines of code (documentation and
/// comments excluded), a method at most 40 lines, and nesting at most 3 levels.
/// </summary>
/// <remarks>
/// Source-level, like the other layout rules, and deliberately "weak but deterministic": it counts
/// braces rather than parsing C#. Generated migrations are excluded. The multi-line signatures it can
/// miss are covered by the code review the budget exists to support.
/// </remarks>
public sealed class CodingStandardTests
{
    private const int MaxCodeLinesPerFile = 150;
    private const int MaxMethodLines = 40;
    private const int MaxNesting = 3;
    private static readonly string[] Modifiers = ["public", "private", "protected", "internal", "static", "async", "sealed", "override"];
    private static readonly string[] TypeKeywords = ["class ", "record ", "interface ", "struct ", "enum "];

    [Fact] //
    public void No_source_file_exceeds_the_code_line_budget()
    {
        var offenders = SourceFiles()
            .Select(file => (File: file, Lines: CodeLines(File.ReadAllLines(file))))
            .Where(entry => entry.Lines > MaxCodeLinesPerFile)
            .Select(entry => $"{entry.File}: {entry.Lines} lines of code")
            .ToArray();

        offenders.Should().BeEmpty($"a file holds at most {MaxCodeLinesPerFile} lines of code");
    }

    [Fact] //
    public void No_method_exceeds_the_length_or_nesting_budget()
    {
        var offenders = new List<string>();
        var methods = 0;

        foreach (var file in SourceFiles())
        {
            var lines = File.ReadAllLines(file);

            for (var index = 0; index < lines.Length; index++)
            {
                if (!LooksLikeMethod(lines[index]))
                {
                    continue;
                }

                var open = index;

                while (open < Math.Min(index + 4, lines.Length) && !lines[open].Contains('{'))
                {
                    open++;
                }

                if (open >= Math.Min(index + 4, lines.Length) || !lines[open].Contains('{'))
                {
                    continue;
                }

                var depth = 0;
                var deepest = 0;
                var end = open;

                for (; end < lines.Length; end++)
                {
                    depth += lines[end].Count(character => character == '{') - lines[end].Count(character => character == '}');
                    deepest = Math.Max(deepest, depth);

                    if (depth <= 0)
                    {
                        break;
                    }
                }

                methods++;
                var length = end - index + 1;

                if (length > MaxMethodLines)
                {
                    offenders.Add($"{file}:{index + 1}: a method of {length} lines");
                }

                if (deepest - 1 > MaxNesting)
                {
                    offenders.Add($"{file}:{index + 1}: nesting of {deepest - 1} levels");
                }

                index = end;
            }
        }

        methods.Should().BeGreaterThan(100, "the rule must be checked against real methods");
        offenders.Should().BeEmpty($"a method is at most {MaxMethodLines} lines and nests at most {MaxNesting} levels");
    }

    private static bool LooksLikeMethod(string line)
    {
        var trimmed = line.Trim();

        if (trimmed.Length == 0 || trimmed.StartsWith("//", StringComparison.Ordinal) || trimmed.EndsWith(';'))
        {
            return false;
        }

        if (!trimmed.Contains('(') || TypeKeywords.Any(keyword => trimmed.Contains(keyword, StringComparison.Ordinal)))
        {
            return false;
        }

        return Modifiers.Any(modifier => trimmed.StartsWith($"{modifier} ", StringComparison.Ordinal));
    }

    private static int CodeLines(IEnumerable<string> lines)
    {
        var count = 0;
        var inBlockComment = false;

        foreach (var raw in lines)
        {
            var line = raw.Trim();

            if (inBlockComment)
            {
                if (line.Contains("*/", StringComparison.Ordinal))
                {
                    inBlockComment = false;
                }

                continue;
            }

            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            if (line.StartsWith("/*", StringComparison.Ordinal))
            {
                inBlockComment = !line.Contains("*/", StringComparison.Ordinal);
                continue;
            }

            count++;
        }

        return count;
    }

    private static IEnumerable<string> SourceFiles()
        => Directory
            .GetFiles(RepoRoot.Combine("src"), "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains("/obj/", StringComparison.Ordinal)
                           && !file.Contains("/bin/", StringComparison.Ordinal)
                           && !file.Contains("/Migrations/", StringComparison.Ordinal));
}
