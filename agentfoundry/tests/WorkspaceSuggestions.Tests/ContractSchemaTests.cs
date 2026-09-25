using AwesomeAssertions;
using System.Text.Json;
using WorkspaceSuggestions.Contracts;
using Xunit;

namespace WorkspaceSuggestions.Tests;

/// <summary>
/// AIWB-01 and AIWB-02: the C# contract and the committed schemas have to agree, in both directions —
/// what we serialize must satisfy the schema, and the schema must still reject what the design forbids.
/// </summary>
public sealed class ContractSchemaTests
{
    [Fact] // AIWB-01
    public void A_result_serializes_to_something_the_result_schema_accepts()
    {
        var result = new SuggestionResult(
            SuggestionStatus.Suggested,
            null,
            [
                new SuggestionOption(
                    [new SuggestionLine(WorkspaceSlot.Monitor, "MONB000PB2KW", "UltraSharp 27", 2, 5_200_000m, "two large displays side by side")],
                    "A wide, stable surface for two displays, with a supportive chair for long sessions."),
            ]);

        Schemas.Evaluate("suggestion.result.schema.json", result).IsValid.Should().BeTrue();
    }

    [Fact] // and the result no longer mentions it at all
    public void A_result_carrying_usage_is_rejected()
    {
        Schemas.EvaluateRaw("suggestion.result.schema.json", """
            {
              "status": "suggested",
              "reason": null,
              "options": [ { "lines": [ { "slot": "Desk", "sku": "X", "quantity": 1, "why": "w" } ], "rationale": "r" } ],
              "usage": { "modelCalls": 2, "inputTokens": 1, "outputTokens": 1, "model": "m", "promptVersion": "v" }
            }
            """).IsValid.Should().BeFalse(
                "the model must not be able to claim what a run cost, even by guessing a plausible shape");
    }

    [Fact] // AIWB-02
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

    [Fact] // the request no longer carries the catalogue, and the schema forbids it returning
    public void A_request_carrying_a_catalogue_is_rejected()
    {
        // The agent searches the catalogue through its MCP tools, so the request must not push it. Pinned
        // because the two trees could otherwise drift back: the schema is what the application serializes
        // against, and `additionalProperties: false` is what makes a stray field a refusal rather than a
        // silent passenger.
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

    [Fact] // the contract enforces the design: no price may cross
    public void A_result_that_quotes_a_price_is_rejected()
    {
        Schemas.EvaluateRaw("suggestion.result.schema.json", """
            {
              "status": "suggested",
              "reason": null,
              "options": [
                { "lines": [ { "slot": "Desk", "sku": "X", "quantity": 1, "why": "w", "price": 266000 } ],
                  "rationale": "r" }
              ]
            }
            """).IsValid.Should().BeFalse();
    }

    [Fact] // the contract enforces the design: no invented slot
    public void A_result_naming_a_slot_outside_the_vocabulary_is_rejected()
    {
        Schemas.EvaluateRaw("suggestion.result.schema.json", """
            {
              "status": "suggested",
              "reason": null,
              "options": [
                { "lines": [ { "slot": "Sofa", "sku": "X", "quantity": 1, "why": "w" } ], "rationale": "r" }
              ]
            }
            """).IsValid.Should().BeFalse();
    }

    [Fact] // the schemas are self-contained, so the slot vocabulary is duplicated on purpose
    public void Every_contract_that_speaks_of_slots_carries_the_same_slot_vocabulary()
    {
        const string vocabulary =
            "\"Desk\", \"Chair\", \"Monitor\", \"Lamp\", \"Plant\", \"CoffeeStation\", \"RelaxZone\"";

        // The run's cost names no slot, so it is the one contract this does not apply to. Naming it rather
        // than dropping the assertion keeps the check able to fail for the three that do.
        var speakingOfSlots = Directory
            .GetFiles(Path.Combine(AppContext.BaseDirectory, "contracts"), "*.json")
            .Where(file => !Path.GetFileName(file).Equals("run-usage.schema.json", StringComparison.Ordinal))
            .ToArray();

        speakingOfSlots.Should().HaveCount(3);

        foreach (var file in speakingOfSlots)
        {
            File.ReadAllText(file).Should().Contain(
                vocabulary,
                $"{Path.GetFileName(file)} must carry the slot vocabulary; the three files are kept in step by this test");
        }
    }

    [Fact] // the check above asked whether a slot was PRESENT, and an extra one went unnoticed for exactly that reason
    public void No_contract_permits_a_slot_or_a_status_the_application_cannot_represent()
    {
        // Found by reading the schemas rather than by this suite: `suggestion.result.schema.json` carried
        // `"enum"` as the first member of its status enum and of its line's slot enum - the JSON key itself,
        // pasted in as a value. The substring assertion above passed throughout, because the vocabulary it
        // looks for was still there. What the stray value DID do is widen the contract: it permitted a status
        // that means nothing and a slot that WorkspaceSlot cannot parse, so a payload could satisfy the
        // contract and fail on the way in. A schema is only a contract where it is as narrow as the code.
        var slots = new[] { "Desk", "Chair", "Monitor", "Lamp", "Plant", "CoffeeStation", "RelaxZone" };
        var statuses = new[] { "suggested", "notWorkspace", "catalogueUnavailable" };

        foreach (var file in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "contracts"), "*.json"))
        {
            var name = Path.GetFileName(file);

            if (name.Equals("run-usage.schema.json", StringComparison.Ordinal))
            {
                continue;
            }

            // Every property called `slot` anywhere in the document, and the status of the result. The
            // request schema names its vocabulary `slotName`, so both names are checked.
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

            if (name.Equals("suggestion.result.schema.json", StringComparison.Ordinal))
            {
                Enums(File.ReadAllText(file))
                    .Single(entry => entry.Path == "/properties/status").Members
                    .Should().BeEquivalentTo(
                        statuses,
                        "a result is either a suggestion or a refusal, and there is no third thing");
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
