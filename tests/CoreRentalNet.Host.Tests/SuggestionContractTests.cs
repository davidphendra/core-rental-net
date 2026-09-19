using System.Text.Json;
using AwesomeAssertions;
using CoreRentalNet.Host.Agents;
using CoreRentalNet.Host.Presentation;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Domain;
using Json.Schema;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// AIWB-19: the application's DTOs still match <c>agentfoundry/shared/contracts</c>.
/// </summary>
/// <remarks>
/// The schemas are the ONE artefact the two trees share, and there is no project reference either way — so
/// this test is the only thing that notices a drift. It reads the same files the agent's own tests read.
/// </remarks>
public sealed class SuggestionContractTests
{
    /// <summary>
    /// Parsed once: <c>JsonSchema.FromText</c> registers a schema globally by its <c>$id</c>, and registering
    /// the same one twice throws.
    /// </summary>
    private static readonly Lazy<IReadOnlyDictionary<string, JsonSchema>> Parsed = new(() =>
        Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "contracts"), "*.json")
            .ToDictionary(
                file => Path.GetFileName(file)!,
                file => JsonSchema.FromText(File.ReadAllText(file))!));

    private const string AgentResult =
        """
        { "status": "suggested", "reason": null,
          "options": [ { "lines": [ { "slot": "Desk", "sku": "DSKB08XN4JDR", "quantity": 1, "why": "a stable surface" } ],
                         "rationale": "A calm, focused setup." } ] }
        """;

    private const string RunUsage =
        """
        { "runUsage": { "modelCalls": 2, "inputTokens": 172000, "outputTokens": 400,
                        "model": "gpt-4.1-mini", "promptVersion": "rephraser.v1+suggestor.v1" } }
        """;

    [Fact]
    public void The_request_the_application_builds_satisfies_the_request_schema()
    {
        var request = new SuggestionRequest(
            "run-1",
            "a quiet corner for two monitors",
            "IDR",
            1_000_000,
            [new SuggestionSlotRule(SlotId.Desk, 1), new SuggestionSlotRule(SlotId.Monitor, 3)],
            [
                new CompactCatalogItem(
                    "DSKB08XN4JDR",
                    "HON Mod Desk Shell, 60 x 30 x 29, Mahogany",
                    CatalogCategory.Desk,
                    null,
                    266_000m,
                    "This 60 inch desk shell is part of the HON Mod Desk Collection.",
                    new CatalogMetadata(["desk", "workstations"], new Dictionary<string, string>(), ["focused work"], ["a move every month"])),
            ]);

        Evaluate("suggestion.request.schema.json", request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void The_result_the_agent_sends_satisfies_the_result_schema()
    {
        EvaluateRaw("suggestion.result.schema.json", AgentResult).IsValid.Should().BeTrue();
    }

    [Fact]
    public void The_run_cost_the_agent_appends_satisfies_the_run_usage_schema()
    {
        // It is no longer a field of the result, so it needs its own pin or the two trees could drift on it
        // silently - which is how the model came to be asked for it in the first place.
        EvaluateRaw("run-usage.schema.json", RunUsage).IsValid.Should().BeTrue();
    }

    [Fact]
    public void The_application_reads_the_agent_result_as_its_own_types()
    {
        var result = JsonSerializer.Deserialize<AgentSuggestionResult>(AgentResult, SuggestionJson.Options)!;

        result.Status.Should().Be(AgentSuggestionStatus.Suggested);
        result.Options.Should().ContainSingle();
        result.Options[0].Lines[0].Sku.Should().Be("DSKB08XN4JDR");

        // The slot vocabulary is PascalCase on the wire — the one place the contract and the application's
        // own camel-case convention differ, which is why the converter sits on the property.
        result.Options[0].Lines[0].Slot.Should().Be(SlotId.Desk);
    }

    [Fact]
    public void The_slot_vocabulary_is_written_the_way_the_contract_declares_it()
    {
        var rule = new SuggestionSlotRule(SlotId.CoffeeStation, 1);
        var line = new AgentSuggestionLine(SlotId.RelaxZone, "BBGB00MV8ON0", 1, "somewhere soft");

        JsonSerializer.Serialize(rule, SuggestionJson.Options).Should().Contain("\"CoffeeStation\"");
        JsonSerializer.Serialize(line, SuggestionJson.Options).Should().Contain("\"RelaxZone\"");
    }

    private static EvaluationResults Evaluate(string file, object instance)
        => Parsed.Value[file].Evaluate(JsonSerializer.SerializeToElement(instance, SuggestionJson.Options));

    private static EvaluationResults EvaluateRaw(string file, string json)
        => Parsed.Value[file].Evaluate(JsonDocument.Parse(json).RootElement);
}
