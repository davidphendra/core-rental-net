using System.Text.Json;
using AwesomeAssertions;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Wire;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// The C# contract and the committed schemas agree, in both directions: what we serialize satisfies the schema,
/// and the schema still refuses what the design forbids.
/// </summary>
/// <remarks>
/// The agent's result schema went with the single-result contract it described. A run now streams typed events,
/// which <see cref="WorkspaceWorkflowContractSchemaTests"/> covers; what is asserted here is the request the
/// application sends, and the rule that every contract speaking of slots speaks the same vocabulary.
/// </remarks>
public sealed class ContractSchemaTests
{
    [Fact]
    public void A_request_serializes_to_something_the_request_schema_accepts()
    {
        var request = new SuggestionRequest(
            "run-1",
            "a quiet corner for two monitors",
            "IDR",
            1_000_000,
            [new SlotRule(WorkspaceSlot.Desk, 1), new SlotRule(WorkspaceSlot.Monitor, 3)]);

        Schemas.Evaluate("suggestion.request.schema.json", request).IsValid.Should().BeTrue();
    }

    [Fact] // the request must not push a catalogue: the agent searches it through its MCP tools
    public void A_request_carrying_a_catalogue_is_rejected()
    {
        Schemas.EvaluateRaw("suggestion.request.schema.json", """
            {
              "runId": "run-1",
              "query": "q",
              "currency": "IDR",
              "slots": [ { "slot": "Desk", "capacity": 1 } ],
              "catalogue": [ { "sku": "X", "name": "n", "category": "desk", "subCategory": null,
                               "pricePerMonth": 1, "description": "d",
                               "metadata": { "tags": [], "attributes": {}, "bestFor": [], "notFor": [] } } ]
            }
            """).IsValid.Should().BeFalse(
                "the catalogue is searched through the agent's tools, not pushed in the request");
    }

    [Fact] // the schemas are self-contained, so the slot vocabulary is duplicated on purpose
    public void Every_contract_that_speaks_of_slots_carries_the_same_slot_vocabulary()
    {
        const string vocabulary =
            "\"Desk\", \"Chair\", \"Monitor\", \"Lamp\", \"Plant\", \"CoffeeStation\", \"RelaxZone\"";

        // Selected by what the file contains rather than by a list of names, so a contract added without the
        // vocabulary is a failure rather than a file nobody remembered to add here.
        var speakingOfSlots = Directory
            .GetFiles(Path.Combine(AppContext.BaseDirectory, "contracts"), "*.json")
            .Where(file => File.ReadAllText(file).Contains(vocabulary, StringComparison.Ordinal))
            .ToArray();

        // Two: the request and the streamed event. Neither the rephraser's reading nor the retrieval names a slot
        // any more — the reading answers in catalogue categories, and the retrieval no longer carries products at
        // all, because they come from the recorded tool answers instead of from a model's report.
        speakingOfSlots.Should().HaveCount(2);

        foreach (var file in speakingOfSlots)
        {
            File.ReadAllText(file).Should().Contain(
                vocabulary,
                $"{Path.GetFileName(file)} must carry the slot vocabulary; the two files are kept in step by this test");
        }
    }

    [Fact] // the check below asked whether a slot was PRESENT, and an extra one went unnoticed for exactly that reason
    public void No_contract_permits_a_slot_the_application_cannot_represent()
    {
        // Found by reading the schemas rather than by a suite: a status enum once carried its own JSON key as a
        // value, which widened the contract to permit a slot that the application could not parse. A schema is
        // only a contract where it is as narrow as the code.
        var slots = new[] { "Desk", "Chair", "Monitor", "Lamp", "Plant", "CoffeeStation", "RelaxZone" };

        foreach (var file in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "contracts"), "*.json"))
        {
            var name = Path.GetFileName(file);

            // Every property called `slot` anywhere in the document. The request schema names its vocabulary
            // `slotName`, so both names are checked.
            foreach (var (path, members) in Enums(File.ReadAllText(file)))
            {
                if (path.EndsWith("/slot", StringComparison.Ordinal)
                    || path.EndsWith("/slotName", StringComparison.Ordinal))
                {
                    members.Should().BeEquivalentTo(
                        slots,
                        $"{name}{path} must be exactly the slot vocabulary, with nothing extra");
                }
            }
        }
    }

    /// <summary>Every enum in a schema document, with the path it sits at.</summary>
    private static IEnumerable<(string Path, IReadOnlyList<string> Members)> Enums(string json)
    {
        var found = new List<(string, IReadOnlyList<string>)>();

        Collect(JsonDocument.Parse(json).RootElement, string.Empty, found);

        return found;
    }

    private static void Collect(
        JsonElement element,
        string path,
        List<(string Path, IReadOnlyList<string> Members)> found)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (element.TryGetProperty("enum", out var values) && values.ValueKind is JsonValueKind.Array)
                {
                    found.Add((path, [.. values.EnumerateArray().Select(value => value.GetString() ?? "(null)")]));
                }

                foreach (var property in element.EnumerateObject())
                {
                    Collect(property.Value, $"{path}/{property.Name}", found);
                }

                break;

            case JsonValueKind.Array:
                var index = 0;

                foreach (var item in element.EnumerateArray())
                {
                    Collect(item, $"{path}[{index++}]", found);
                }

                break;
        }
    }
}
