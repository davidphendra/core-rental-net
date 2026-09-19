using AwesomeAssertions;
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
                    [new SuggestionLine(WorkspaceSlot.Monitor, "MONB000PB2KW", 2, "two large displays side by side")],
                    "A wide, stable surface for two displays, with a supportive chair for long sessions."),
            ]);

        Schemas.Evaluate("suggestion.result.schema.json", result).IsValid.Should().BeTrue();
    }

    [Fact] // the run's cost is code-written, and has a contract of its own
    public void A_run_usage_report_serializes_to_something_the_run_usage_schema_accepts()
    {
        // It stopped being a field of the result because the model cannot observe a call count. It is still
        // part of the answer, and still has to be pinned, or the two trees could drift apart on it silently.
        var report = new RunUsageReport(new RunUsage(2, 172_000, 900, "gpt-4.1-mini", "rephraser.v1+suggestor.v1"));

        Schemas.Evaluate("run-usage.schema.json", report).IsValid.Should().BeTrue();
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
            [new SlotRule(WorkspaceSlot.Desk, 1), new SlotRule(WorkspaceSlot.Monitor, 3)],
            [
                new CatalogueItem(
                    "DSKB08XN4JDR",
                    "HON Mod Desk Shell, 60 x 30 x 29, Mahogany",
                    "desk",
                    null,
                    266_000,
                    "This 60 inch desk shell is part of the HON Mod Desk Collection.",
                    new CatalogueMetadata(
                        ["desk", "workstations"],
                        new Dictionary<string, string> { ["material"] = "Metal" },
                        ["focused work"],
                        ["a move every month"])),
            ]);

        Schemas.Evaluate("suggestion.request.schema.json", request).IsValid.Should().BeTrue();
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
}
