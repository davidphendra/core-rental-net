using System.Text.Json;
using AgentFoundry.WorkspaceSuggestions.Vocabularies;
using AwesomeAssertions;
using Xunit;

namespace AgentFoundry.Tests;

/// <summary>
/// The schemas the application compiles against, and the vocabularies in them.
/// </summary>
/// <remarks>
/// The contract is the only artefact the two solutions share, so the lists in it are asserted against
/// the lists in the code: a stage or a status added on one side and not the other would otherwise be
/// discovered by the application at run time, which is the worst place to discover it.
/// </remarks>
public sealed class ContractSchemaTests
{
    [Fact]
    public void The_three_schemas_are_where_the_application_expects_them()
    {
        foreach (var name in new[] { "request", "event", "result" })
        {
            File.Exists(Path.Combine(Contracts, $"workspace-suggestion.{name}.schema.json"))
                .Should().BeTrue($"the {name} schema is part of the contract");
        }
    }

    [Fact]
    public void The_stage_vocabulary_is_the_one_the_schema_closes()
    {
        var stages = Enumerated("event", "properties", "stage");

        stages.Should().BeEquivalentTo(Stages.InOrder);
    }

    [Fact]
    public void The_status_vocabulary_is_the_one_the_schema_closes()
    {
        var statuses = Enumerated("result", "properties", "status");

        statuses.Should().BeEquivalentTo([ReasonCodes.Ok, ReasonCodes.Exhausted, ReasonCodes.Rejected]);
    }

    [Fact]
    public void The_slot_vocabulary_is_the_one_the_request_schema_closes()
    {
        // The slot list is the one vocabulary that lives only in the schema, because the application
        // owns it: the agent validates against this list rather than keeping a second copy.
        var slots = Enumerated("request", "$defs", "slotRule", "properties", "slot");

        slots.Should().BeEquivalentTo(
            ["Desk", "Chair", "Monitor", "Lamp", "Plant", "CoffeeStation", "RelaxZone"]);
    }

    private static string[] Enumerated(string schema, params string[] path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(Contracts, $"workspace-suggestion.{schema}.schema.json")));

        var element = document.RootElement;
        foreach (var step in path.Concat(["enum"]))
        {
            element = element.GetProperty(step);
        }

        return [.. element.EnumerateArray().Select(value => value.GetString()!)];
    }

    /// <summary>Where the schemas live, found by walking up from the test binary to the repository.</summary>
    private static string Contracts
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "agentfoundry")))
            {
                directory = directory.Parent;
            }

            directory.Should().NotBeNull("the test binary runs inside the repository");

            return Path.Combine(directory!.FullName, "agentfoundry", "shared", "contracts");
        }
    }
}
