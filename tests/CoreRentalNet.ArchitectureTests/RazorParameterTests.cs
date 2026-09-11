using System.Text.RegularExpressions;
using AwesomeAssertions;
using Xunit;

namespace CoreRentalNet.ArchitectureTests;

/// <summary>
/// A component parameter typed as a string takes a bare attribute value as text, so
/// <c>Address="Session.Error"</c> passes those eleven characters rather than the error they name.
/// A value that is a dotted path is never meant as text, and this catches it wherever it appears.
/// </summary>
/// <remarks>
/// The limit, stated rather than left to be discovered: a value that is a plain identifier
/// (<c>Title="title"</c>) cannot be told from a literal that was meant as a literal without
/// resolving types properly, so this checks dotted paths, where the intent is unambiguous.
/// </remarks>
public sealed class RazorParameterTests
{
    private static string Components => RepoRoot.Combine("src", "Host", "CoreRentalNet.Host", "Components");

    private static readonly string[] Dotted = [@"^[A-Za-z_]\w*(\.[\w]+)+$"];

    [Fact] // UI-09
    public void A_string_parameter_is_never_given_a_bare_dotted_value()
    {
        var offenders = new List<string>();

        foreach (var file in Directory.GetFiles(Components, "*.razor", SearchOption.AllDirectories))
        {
            // Only elements that name a component: markup elements are lower case by convention.
            foreach (Match tag in Regex.Matches(File.ReadAllText(file), @"<([A-Z][A-Za-z0-9_]*)\b[^>]*?>", RegexOptions.Singleline))
            {
                var component = tag.Groups[1].Value;
                var source = FindComponent(component);

                if (source is null)
                {
                    continue;
                }

                foreach (Match attribute in Regex.Matches(tag.Value, @"\b([A-Za-z][A-Za-z0-9_]*)\s*=\s*""([^""]*)"""))
                {
                    var name = attribute.Groups[1].Value;
                    var value = attribute.Groups[2].Value;

                    if (value.StartsWith('@') || !Regex.IsMatch(value, Dotted[0]))
                    {
                        continue;
                    }

                    if (DeclaresStringParameter(File.ReadAllText(source), name))
                    {
                        offenders.Add($"{Path.GetFileName(file)}: {component}.{name}=\"{value}\"");
                    }
                }
            }
        }

        offenders.Should().BeEmpty(
            "a string parameter given a bare value receives it as text, so the words of an expression are shown to the customer");
    }

    /// <summary>The component's own file, so its parameter's type can be read.</summary>
    private static string? FindComponent(string component)
        => Directory.GetFiles(Components, $"{component}.razor", SearchOption.AllDirectories).FirstOrDefault();

    private static bool DeclaresStringParameter(string source, string parameter)
        => Regex.IsMatch(
            source,
            @"\[Parameter\][^;{]*?\bpublic\s+string\??\s+" + Regex.Escape(parameter) + @"\s*\{",
            RegexOptions.Singleline);
}
