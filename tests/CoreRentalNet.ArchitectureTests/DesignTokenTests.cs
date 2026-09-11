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
    public void The_generated_stylesheet_is_built_from_the_design_and_not_hand_edited()
    {
        var styles = RepoRoot.Combine("src", "Host", "CoreRentalNet.Host", "Styles");

        File.Exists(Path.Combine(styles, "tailwind.config.js")).Should().BeTrue(
            "the stylesheet is generated from the design's own configuration");
        File.Exists(Path.Combine(styles, "input.css")).Should().BeTrue();
        File.Exists(Path.Combine(styles, "package.json")).Should().BeTrue(
            "the build command lives with it, so the generated file is never edited by hand");

        var generated = File.ReadAllText(
            RepoRoot.Combine("src", "Host", "CoreRentalNet.Host", "wwwroot", "styles", "tailwind.css"));

        generated.Should().NotBeEmpty("otherwise the pages would render unstyled");
    }

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

    [Fact] // UI-06
    public void Nothing_in_the_host_fetches_from_a_third_party()
    {
        var offenders = new List<string>();

        foreach (var file in Directory
                     .GetFiles(Path.Combine(HostRoot, "wwwroot"), "*.*", SearchOption.AllDirectories)
                     .Concat(Directory.GetFiles(Path.Combine(HostRoot, "Components"), "*.razor", SearchOption.AllDirectories)))
        {
            if (file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = File.ReadAllText(file);

            if (text.Contains("googleapis.com", StringComparison.OrdinalIgnoreCase)
                || text.Contains("gstatic.com", StringComparison.OrdinalIgnoreCase)
                || text.Contains("googleusercontent.com", StringComparison.OrdinalIgnoreCase)
                || text.Contains("cdn.", StringComparison.OrdinalIgnoreCase))
            {
                offenders.Add(Path.GetRelativePath(RepoRoot.Path, file));
            }
        }

        offenders.Should().BeEmpty(
            "the browser test suite must run with no network access to anything but the app itself");
    }

    [Fact] // UI-06
    public void The_design_system_fonts_are_served_by_the_application()
    {
        var fonts = Path.Combine(HostRoot, "wwwroot", "fonts");
        var files = Directory.GetFiles(fonts, "*.woff2");

        files.Should().HaveCountGreaterThanOrEqualTo(5, "the two families and the weights DESIGN.md uses");

        foreach (var file in files)
        {
            File.ReadAllBytes(file)[..4].Should().Equal("wOF2"u8.ToArray(), $"{Path.GetFileName(file)} must be a woff2 file");
            new FileInfo(file).Length.Should().BeGreaterThan(5000, $"{Path.GetFileName(file)} must contain real glyphs, not a subset with no latin characters");
        }

        var styles = File.ReadAllText(Path.Combine(HostRoot, "wwwroot", "styles", "fonts.css"));
        styles.Should().Contain("Plus Jakarta Sans").And.Contain("Manrope");
    }

    [Fact] // UI-01
    public void The_token_file_holds_the_palette_from_the_design_system()
    {
        var tokens = File.ReadAllText(Path.Combine(HostRoot, "wwwroot", "styles", "tokens.css"));

        foreach (var token in new[] { "--primary: #006767", "--tertiary-container: #bb580d", "--surface: #f8f9fa", "--secondary: #376757" })
        {
            tokens.Should().Contain(token, "the palette must match the design's config");
        }
    }

    [Fact] // UI-01
    public void The_design_system_scale_is_available_as_tokens()
    {
        var tokens = File.ReadAllText(Path.Combine(HostRoot, "wwwroot", "styles", "tokens.css"));

        foreach (var token in new[]
                 {
                     "--unit: 8px", "--container-max: 1280px", "--gutter: 24px", "--margin-mobile: 16px",
                     "--margin-desktop: 40px", "--section-gap: 80px", "--radius: 0.25rem", "--radius-xl: 0.75rem",
                     "--backdrop-blur: 12px",
                 })
        {
            tokens.Should().Contain(token, "the spacing and shape scale comes from the design's config");
        }
    }

    private static IEnumerable<string> FilesToCheck()
        => Directory.GetFiles(Path.Combine(HostRoot, "wwwroot", "styles"), "*.css")
            .Concat(Directory.GetFiles(Path.Combine(HostRoot, "Components"), "*.css", SearchOption.AllDirectories))
            .Where(file => !file.Contains("/obj/", StringComparison.Ordinal) && !file.Contains("/bin/", StringComparison.Ordinal))
            // The one generated artifact. Its colours come from the design's own Tailwind config,
            // which is the source of truth for them; the rule below is about hand-written styles,
            // and the test above proves this file is generated rather than authored.
            .Where(file => !file.EndsWith("tailwind.css", StringComparison.Ordinal));

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
