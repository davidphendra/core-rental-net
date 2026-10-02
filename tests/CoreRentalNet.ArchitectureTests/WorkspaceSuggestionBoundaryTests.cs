using System.Text.RegularExpressions;
using AwesomeAssertions;
using Xunit;

namespace CoreRentalNet.ArchitectureTests;

/// <summary>
/// The suggestion run's internal boundaries, read from the source: the pipeline owns the events, and the
/// endpoint owns none of them.
/// </summary>
/// <remarks>
/// Source-level like the other layout rules, because what is asserted is where a name is written rather than
/// what a container holds. The switch that used to live in the controller is what these two rules keep out.
/// </remarks>
public sealed class WorkspaceSuggestionBoundaryTests
{
    [Fact] // a new event kind cannot arrive without a handler
    public void Every_agent_event_is_handled_by_exactly_one_handler()
    {
        var eventTypes = Directory
            .GetFiles(Suggestions("Application", Path.Combine("Agent", "Events")), "WorkspaceSuggestion*Event.cs")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.Equals(name, "WorkspaceSuggestionEvent", StringComparison.Ordinal))
            .ToArray();

        var handlerFiles = Directory.GetFiles(
            Suggestions("Application", Path.Combine("Run", "Handlers")), "*StreamEventHandler.cs");

        eventTypes.Should().HaveCountGreaterThanOrEqualTo(3, "the closed hierarchy has three events to route");
        handlerFiles.Should().HaveCountGreaterThanOrEqualTo(3, "the chain has one handler per event");

        foreach (var eventType in eventTypes)
        {
            var handlers = handlerFiles
                .Where(file => File.ReadAllText(file).Contains($"is not {eventType}", StringComparison.Ordinal))
                .ToArray();

            handlers.Should().ContainSingle($"{eventType} must be handled by exactly one handler");
        }
    }

    [Fact] // interpretation is not the endpoint's
    public void The_endpoint_names_no_agent_event_or_run_state()
    {
        string[] forbidden =
        [
            "WorkspaceSuggestionEvent",
            "WorkspaceSuggestionRunLedger",
            "WorkspaceSuggestionNarrativeFieldReader",
            "WorkspaceSuggestionOutputHygiene",
        ];

        var controller = File.ReadAllText(
            RepoRoot.Combine("src", "Host", "CoreRentalNet.Host", "Controllers", "BuilderController.cs"));

        foreach (var name in forbidden)
        {
            // <b>Matched as a name and not as a substring.</b> The controller legitimately names
            // <c>ServerSentWorkspaceSuggestionEventWriter</c>, which contains this name's letters without naming
            // the type the rule is about - so the check is where the name stands alone.
            Regex.IsMatch(controller, $@"\b{name}\b")
                .Should().BeFalse($"the run's interpretation belongs to the module, not the endpoint: {name}");
        }
    }

    [Fact] // nor is the rule vacuous: the run still interprets them
    public void The_run_and_its_pipeline_still_name_them()
    {
        File.ReadAllText(Suggestions("Application", "Run", "WorkspaceSuggestionRunState.cs"))
            .Should().Contain("WorkspaceSuggestionRunLedger").And.Contain("WorkspaceSuggestionNarrativeFieldReader");

        File.ReadAllText(Suggestions(
                "Application", Path.Combine("Run", "Handlers"), "IWorkspaceSuggestionStreamEventHandler.cs"))
            .Should().Contain("WorkspaceSuggestionEvent");

        File.ReadAllText(Suggestions(
                "Application", Path.Combine("Run", "Handlers"), "WorkspaceSuggestionNarrativeDeltaStreamEventHandler.cs"))
            .Should().Contain("WorkspaceSuggestionOutputHygiene");
    }

    private static string Suggestions(string layer, string scope, string? file = null)
        => file is null
            ? RepoRoot.Combine("src", "Modules", "Workspace", $"CoreRentalNet.Modules.Workspace.{layer}", "Suggestions", scope)
            : RepoRoot.Combine("src", "Modules", "Workspace", $"CoreRentalNet.Modules.Workspace.{layer}", "Suggestions", scope, file);
}
