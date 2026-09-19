using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Agents.AI;
using WorkspaceSuggestions.Agents;
using WorkspaceSuggestions.Contracts;
using Xunit;

namespace WorkspaceSuggestions.Tests;

/// <summary>
/// AIWB-05 to AIWB-08: the rephraser's contract — a specification naming only real slots, carrying no
/// product, a verdict that is a result and not an error, and a ceiling that is never invented.
/// </summary>
/// <remarks>
/// Run against the roster's own rephraser over a fake chat client, so the profile, its embedded prompt and
/// its output format are the things under test — not the model's judgement, which the evaluation tier grades.
/// </remarks>
public sealed class RephraserSpecTests
{
    [Fact] // AIWB-05
    public async Task A_vague_sentence_yields_a_specification_naming_only_slots_that_exist()
    {
        var spec = await Spec("""
            { "status": "spec", "reason": null, "ceilingMonthly": null,
              "slots": [ { "slot": "Desk", "quantity": 1, "purpose": "a wide, stable surface" },
                         { "slot": "Monitor", "quantity": 2, "purpose": "two screens side by side" } ],
              "constraints": [ "a small room" ] }
            """);

        spec.Status.Should().Be(SpecificationStatus.Spec);
        spec.Slots.Should().HaveCount(2);
        spec.Slots.Select(slot => slot.Slot).Should().OnlyContain(slot => Enum.IsDefined(slot));
        spec.Constraints.Should().Contain("a small room");
    }

    [Fact] // AIWB-05, the other half: the vocabulary is the request's, and nothing else parses
    public async Task A_specification_naming_a_slot_outside_the_vocabulary_is_refused()
    {
        var act = async () => await Spec("""
            { "status": "spec", "reason": null, "ceilingMonthly": null,
              "slots": [ { "slot": "Sofa", "quantity": 1, "purpose": "somewhere to sit" } ],
              "constraints": [] }
            """);

        await act.Should().ThrowAsync<JsonException>();
    }

    [Fact] // AIWB-06
    public void A_specification_carrying_a_sku_a_price_or_a_product_name_is_rejected()
    {
        const string withSlot = """
            { "status": "spec", "reason": null, "ceilingMonthly": null,
              "slots": [ { "slot": "Desk", "quantity": 1, "purpose": "a wide, stable surface"%EXTRA% } ],
              "constraints": [] }
            """;

        // The specification is about what the customer wants, never which product supplies it, so none of
        // these properties exists on it and the schema refuses any of them outright.
        Schemas.EvaluateRaw("workspace-spec.schema.json", withSlot.Replace("%EXTRA%", "")).IsValid.Should().BeTrue();

        foreach (var extra in new[] { ", \"sku\": \"DSKB08XN4JDR\"", ", \"price\": 266000", ", \"name\": \"HON Mod\"" })
        {
            Schemas.EvaluateRaw("workspace-spec.schema.json", withSlot.Replace("%EXTRA%", extra)).IsValid
                .Should().BeFalse($"a specification may not carry {extra.Trim()}");
        }
    }

    [Fact] // AIWB-07
    public async Task An_off_topic_sentence_yields_the_typed_verdict_rather_than_an_error()
    {
        var spec = await Spec("""
            { "status": "notWorkspace", "reason": "not about furnishing a workspace",
              "ceilingMonthly": null, "slots": [], "constraints": [] }
            """);

        spec.Status.Should().Be(SpecificationStatus.NotWorkspace);
        spec.Reason.Should().NotBeNullOrWhiteSpace();
        spec.Slots.Should().BeEmpty();
    }

    [Fact] // AIWB-07, the other half: the verdict is a result, and the schema says its reason is required
    public void The_verdict_passes_the_spec_schema_and_a_verdict_without_a_reason_does_not()
    {
        Schemas.EvaluateRaw("workspace-spec.schema.json", """
            { "status": "notWorkspace", "reason": "not about furnishing a workspace",
              "ceilingMonthly": null, "slots": [], "constraints": [] }
            """).IsValid.Should().BeTrue();

        Schemas.EvaluateRaw("workspace-spec.schema.json", """
            { "status": "notWorkspace", "ceilingMonthly": null, "slots": [], "constraints": [] }
            """).IsValid.Should().BeFalse("a refusal has to say why");
    }

    [Fact] // AIWB-08
    public async Task A_stated_ceiling_survives_and_an_absent_one_is_not_invented()
    {
        var stated = await Spec("""
            { "status": "spec", "reason": null, "ceilingMonthly": 1500000,
              "slots": [ { "slot": "Desk", "quantity": 1, "purpose": "a wide, stable surface" } ],
              "constraints": [] }
            """);

        stated.CeilingMonthly.Should().Be(1_500_000);

        var absent = await Spec("""
            { "status": "spec", "reason": null, "ceilingMonthly": null,
              "slots": [ { "slot": "Desk", "quantity": 1, "purpose": "a wide, stable surface" } ],
              "constraints": [] }
            """);

        absent.CeilingMonthly.Should().BeNull("the agent must not invent a ceiling the customer never named");
    }

    private static async Task<WorkspaceSpec> Spec(string reply)
    {
        var agent = AgentFactory.Build(AgentRoster.Rephraser, new FixedChatClient(reply));

        AgentResponse<WorkspaceSpec> response = await agent.RunAsync<WorkspaceSpec>("a quiet corner for two screens");

        return response.Result;
    }
}
