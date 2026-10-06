using System.Collections.Immutable;
using System.Text.Json;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Xunit;

namespace CoreRentalNet.ArchitectureTests;

/// <summary>
/// Every setting the host reads is declared in the file that ships it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The failure this exists for is silent.</b> A reader whose key is missing from <c>appsettings.json</c>
/// takes its fallback, and where the fallback is <c>false</c> the feature simply disappears: no error, no
/// warning, a clean start, and a page that says nothing about why. That is how the workspace builder's AI
/// section went missing — its keys were read in code and declared in no file.
/// </para>
/// <para>
/// <b>It reads the code rather than a list.</b> The keys are whatever string literals the host hands to
/// configuration, so a key added or renamed in code is checked as it is written. A list maintained by hand
/// would have exactly the hole the file has.
/// </para>
/// <para>
/// <b>An absent key and a blank value are not the same thing</b>, which is why the file is allowed to declare
/// a setting as empty: that is a deliberate statement about the value, and this guards the omission.
/// </para>
/// </remarks>
public sealed class HostConfigurationTests
{
    /// <summary>The three permissions whose claim keys are read through a template instead of written out.</summary>
    /// <remarks>
    /// Written here rather than imported, because this project deliberately does not reference the host: a test
    /// that read the constants would follow a renamed section instead of failing on it. The template below is
    /// expanded over these names, so a permission the file has stopped declaring fails this test.
    /// </remarks>
    private static readonly ImmutableArray<string> ClaimSections = ["CatalogRead", "SimilaritySearch", "WorkspaceSuggestion"];

    /// <summary>The one key the host reads that no committed file may hold.</summary>
    /// <remarks>
    /// The identity secret comes from user secrets locally and from the environment otherwise, and
    /// <see cref="CommittedConfigurationTests"/> is the guard that keeps it out of every committed file.
    /// </remarks>
    private static readonly ImmutableArray<string> DeclaredNowhere = ["Auth0:ClientSecret", "VectorEmbedding:ApiKey"];

    /// <summary>Build output and the styles vendored beside the application are not the application.</summary>
    private static readonly ImmutableArray<string> ExcludedSegments =
    [
        $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}",
    ];

    /// <summary>A key as it is written: a capitalised section, then one colon-separated path.</summary>
    /// <remarks>
    /// Capitalisation is what separates a key from the strings around it. Measured over the host, this matches
    /// 35 literals and one false positive — <c>"refused:{status}"</c> — which the rule excludes on its own,
    /// because a section is never lower case.
    /// </remarks>
    private const string KeyLiteralPattern = "\"[A-Z][A-Za-z0-9]*(:[A-Za-z0-9{}$]+)+\"";

    [Fact]
    public void Every_setting_the_host_reads_is_declared_in_the_file_that_ships_it()
    {
        var declared = Declared();

        var read = HostSources()
            .SelectMany(LiteralsIn)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var templates = read.Where(key => key.Contains('{', StringComparison.Ordinal)).ToArray();

        // Non-vacuity first. A pattern that stopped matching would find no keys and so report none missing, and
        // this is the assertion that fails instead.
        templates.Should().HaveCount(2, "the claim keys are read through exactly two templates");
        read.Should().HaveCountGreaterThanOrEqualTo(
            30,
            "the host reads at least thirty settings by name, so finding fewer means the search is broken");

        var required = read
            .Except(templates, StringComparer.Ordinal)
            .Concat(templates.SelectMany(Expand))
            .Except(DeclaredNowhere, StringComparer.Ordinal)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var missing = required.Where(key => !declared.Contains(key)).ToArray();

        missing.Should().BeEmpty(
            "every setting the host reads has to be declared in appsettings.json, because a key that is absent "
            + "silently takes its fallback -- and for a feature whose fallback is off, that is the feature gone");
    }

    /// <summary>The keys one template stands for: one reading per permission.</summary>
    private static IEnumerable<string> Expand(string template)
        => ClaimSections.Select(section => template.Replace("{section}", section, StringComparison.Ordinal));

    /// <summary>Every key the host's settings file declares, with its nesting flattened into colons.</summary>
    /// <remarks>
    /// Ordinal throughout, because configuration keys are not text a reader compares: two keys that differ in
    /// case are two keys, and a comparison that ignored that would report a missing setting as present.
    /// </remarks>
    private static HashSet<string> Declared()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(
            RepoRoot.Combine("src", "Host", "CoreRentalNet.Host", "appsettings.json")));

        return new HashSet<string>(Keys(document.RootElement, parent: string.Empty), StringComparer.Ordinal);
    }

    /// <summary>The keys at and below one node: an object nests, and anything else is a value.</summary>
    private static IEnumerable<string> Keys(JsonElement element, string parent)
    {
        if (element.ValueKind is not JsonValueKind.Object)
        {
            yield return parent;
            yield break;
        }

        foreach (var property in element.EnumerateObject())
        {
            var key = parent.Length == 0 ? property.Name : $"{parent}:{property.Name}";

            foreach (var nested in Keys(property.Value, key))
            {
                yield return nested;
            }
        }
    }

    /// <summary>Every C# and Razor file the host ships, without build output or vendored styles.</summary>
    private static IEnumerable<string> HostSources()
    {
        var host = RepoRoot.Combine("src", "Host");

        return Directory.GetFiles(host, "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(host, "*.razor", SearchOption.AllDirectories))
            .Where(file => !ExcludedSegments.Any(segment => file.Contains(segment, StringComparison.Ordinal)));
    }

    /// <summary>The bare keys one file states, with the quotes taken off.</summary>
    private static IEnumerable<string> LiteralsIn(string file)
        => Regex.Matches(File.ReadAllText(file), KeyLiteralPattern)
            .Select(match => match.Value[1..^1]);
}
