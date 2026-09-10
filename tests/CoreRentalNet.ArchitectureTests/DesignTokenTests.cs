using AwesomeAssertions;
using Xunit;

namespace CoreRentalNet.ArchitectureTests;

/// <summary>
/// Colour comes from DESIGN.md by way of tokens.css and from nowhere else. A hex literal in a
/// component is how a design system quietly stops being one.
/// </summary>
public sealed class DesignTokenTests
{
    private static string HostRoot => RepoRoot.Combine("src", "Host", "CoreRentalNet.Host");

    [Fact] // UI-01
    public void No_stylesheet_or_component_hardcodes_a_colour()
    {
        var tokensFile = Path.Combine(HostRoot, "wwwroot", "styles", "tokens.css");
        var offenders = new List<string>();

        foreach (var file in FilesToCheck())
        {
            if (string.Equals(file, tokensFile, StringComparison.Ordinal))
            {
                continue;
            }

            var lines = File.ReadAllLines(file);

            for (var index = 0; index < lines.Length; index++)
            {
                if (ContainsColourLiteral(lines[index]))
                {
                    offenders.Add($"{Path.GetRelativePath(RepoRoot.Path, file)}:{index + 1}: {lines[index].Trim()}");
                }
            }
        }

        offenders.Should().BeEmpty(
            "every colour must come from a custom property in tokens.css (ADR-0012)");
    }

    [Fact] // UI-01
    public void The_token_file_holds_the_palette_from_the_design_system()
    {
        var tokens = File.ReadAllText(Path.Combine(HostRoot, "wwwroot", "styles", "tokens.css"));

        foreach (var token in new[] { "--primary: #006767", "--tertiary-container: #bb580d", "--surface: #f8f9fa", "--secondary: #376757" })
        {
            tokens.Should().Contain(token, "the palette must match DESIGN.md");
        }
    }

    [Fact] // UI-01
    public void The_design_system_scale_is_available_as_tokens()
    {
        var tokens = File.ReadAllText(Path.Combine(HostRoot, "wwwroot", "styles", "tokens.css"));

        foreach (var token in new[]
                 {
                     "--unit: 8px", "--container-max: 1280px", "--gutter: 24px", "--margin-mobile: 16px",
                     "--margin-desktop: 40px", "--section-gap: 80px", "--radius: 0.5rem", "--radius-xl: 1.5rem",
                     "--backdrop-blur: 12px",
                 })
        {
            tokens.Should().Contain(token, "the spacing and shape scale comes from DESIGN.md");
        }
    }

    private static IEnumerable<string> FilesToCheck()
        => Directory.GetFiles(Path.Combine(HostRoot, "wwwroot", "styles"), "*.css")
            .Concat(Directory.GetFiles(Path.Combine(HostRoot, "Components"), "*.css", SearchOption.AllDirectories))
            .Where(file => !file.Contains("/obj/", StringComparison.Ordinal) && !file.Contains("/bin/", StringComparison.Ordinal));

    private static bool ContainsColourLiteral(string line)
    {
        var trimmed = line.TrimStart();

        if (trimmed.StartsWith("/*", StringComparison.Ordinal) || trimmed.StartsWith("*", StringComparison.Ordinal))
        {
            return false;
        }

        if (line.Contains("rgba?(", StringComparison.Ordinal) || line.Contains("hsl(", StringComparison.Ordinal))
        {
            return true;
        }

        for (var index = 0; index < line.Length - 3; index++)
        {
            if (line[index] != '#')
            {
                continue;
            }

            var hexDigits = 0;

            while (index + 1 + hexDigits < line.Length && Uri.IsHexDigit(line[index + 1 + hexDigits]))
            {
                hexDigits++;
            }

            if (hexDigits is 3 or 4 or 6 or 8)
            {
                return true;
            }
        }

        return false;
    }
}
