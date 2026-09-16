using System.Reflection;
using AwesomeAssertions;
using Xunit;

namespace CoreRentalNet.ArchitectureTests;

/// <summary>
/// Source-level rules about what a component is allowed to know.
/// </summary>
/// <remarks>
/// The Component layer and the composition root live in the same assembly, so a compiled-type rule
/// cannot tell them apart by namespace alone. These read the markup instead, which is where the
/// dependency is written.
/// </remarks>
public sealed class ComponentCouplingTests
{
    [Fact] // ARC-07
    public void No_component_injects_an_application_handler_by_its_concrete_type()
    {
        var injected = ComponentInjections();

        // Guards against the rule silently going vacuous: if nobody injects anything, it proves nothing.
        injected.Should().NotBeEmpty("the components inject the application, so there is a seam to check");

        injected
            .Where(entry => IsConcreteHandlerInjection(entry.Line))
            .Select(entry => $"{entry.File}: {entry.Line}")
            .Should().BeEmpty(
                "a component names the operation's interface; which handler implements it is the application's business");
    }

    /// <summary>An injected type that is a handler class rather than the interface a component may name.</summary>
    private static bool IsConcreteHandlerInjection(string line)
    {
        var type = line.Split(' ', StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1);

        return type is not null
            && type.EndsWith("Handler", StringComparison.Ordinal)
            && !type.StartsWith('I');
    }

    [Fact] // ARC-07
    public void The_handlers_the_rule_is_about_still_exist()
    {
        // The rule above passes trivially if nothing is called a handler any more, so the handlers it
        // is about are asserted to exist rather than assumed.
        var handlers = Directory
            .GetFiles(AppContext.BaseDirectory, "CoreRentalNet.Modules.*.Application.dll")
            .Where(path => !path.EndsWith(".resources.dll", StringComparison.Ordinal))
            .Select(Assembly.LoadFrom)
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.Name.EndsWith("Handler", StringComparison.Ordinal))
            .ToArray();

        handlers.Should().NotBeEmpty();
    }

    [Fact] // ARC-08
    public void No_component_carries_an_inline_style_or_an_inline_script()
    {
        var offenders = new List<string>();

        foreach (var file in ComponentFiles())
        {
            var lines = File.ReadAllLines(file);

            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index];

                if (line.Contains("style=\"", StringComparison.Ordinal))
                {
                    offenders.Add($"{file}:{index + 1} inline style attribute");
                }

                // A script tag with no src is an inline script block; the framework's own boot script
                // always names one.
                if (line.Contains("<script", StringComparison.Ordinal)
                    && !line.Contains("src=", StringComparison.Ordinal))
                {
                    offenders.Add($"{file}:{index + 1} inline script block");
                }
            }
        }

        offenders.Should().BeEmpty(
            "an inline style or script cannot be admitted without unsafe-inline, which is the weakness the CSP removes");
    }

    [Fact] // ARC-09
    public void No_component_loads_a_resource_from_another_origin()
    {
        // Only the attributes that cause a load, so a comment may still link to a document.
        string[] markers = ["src=\"http", "href=\"http", "url(http", "url('http"];

        var offenders = ComponentFiles()
            .SelectMany(file => File.ReadAllLines(file).Select(line => (File: file, Line: line.Trim())))
            .Where(entry => markers.Any(marker => entry.Line.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            .Select(entry => $"{entry.File}: {entry.Line}")
            .ToArray();

        offenders.Should().BeEmpty("the policy admits this origin only, so a component must load from it");
    }

    private static IReadOnlyList<string> ComponentFiles()
        => Directory.GetFiles(
            RepoRoot.Combine("src", "Host", "CoreRentalNet.Host", "Components"),
            "*.razor",
            SearchOption.AllDirectories);

    private static IReadOnlyList<(string File, string Line)> ComponentInjections()
        => ComponentFiles()
            .SelectMany(file => File.ReadAllLines(file)
                .Select(line => line.Trim())
                .Where(line => line.StartsWith("@inject", StringComparison.Ordinal))
                .Select(line => (File: file, Line: line)))
            .ToArray();
}
