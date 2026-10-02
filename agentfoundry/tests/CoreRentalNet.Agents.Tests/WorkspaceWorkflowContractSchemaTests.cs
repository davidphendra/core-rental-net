using AwesomeAssertions;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Wire;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.StreamingEvents;
using Xunit;
using CoreRentalNet.Agents.Shared.Serialization;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// The workflow's own contracts against their committed schemas, in both directions: what the stages produce
/// must satisfy the schema, and the schema must still refuse what the design forbids.
/// </summary>
/// <remarks>
/// These are the contracts the pipeline introduced, so the schemas are what keep a later change from widening
/// them: a reading that names a product, a retrieval that invents a slot, a review that decides the next step, or
/// an event that is not one of the five the caller knows.
/// </remarks>
public sealed class WorkspaceWorkflowContractSchemaTests
{
    [Fact]
    public void A_requirement_reading_serializes_to_something_its_schema_accepts()
    {
        var requirementExpansion = WorkspaceRequirementExpansionFixtures.Valid();

        Schemas.Evaluate("workspace-retrieval-requirement.schema.json", requirementExpansion).IsValid
            .Should().BeTrue();
    }

    [Fact]
    public void A_requirement_reading_carrying_a_product_is_rejected()
    {
        Schemas.EvaluateRaw("workspace-retrieval-requirement.schema.json", """
            { "original_query": "a desk",
              "workspace_intent": { "purpose": [], "style": [], "experience": [], "usage": [] },
              "total_budget": { "amount": null, "currency": null, "is_explicit": false },
              "categories": {
                "desk": { "relevant": true, "retrieval_query": "a surface", "search_terms": ["desk"],
                          "synonyms": [], "semantic_concepts": [], "sku": "DSKB08XN4JDR",
                          "budget": { "max_amount": null, "currency": null, "is_explicit": false, "is_derived": false } }
              } }
            """).IsValid.Should().BeFalse(
                "the reading decides what is wanted and how to find it, never which product supplies it");
    }

    [Fact]
    public void A_retrieval_serializes_to_something_the_retrieval_schema_accepts()
    {
        var retrieval = new CatalogueProductRetrievalResult(
            IsAvailable: true,
            UnavailableReason: null,
            ComponentSearchOutcomes:
            [
                new WorkspaceComponentSearchOutcome(
                    "search_catalogue", WorkspaceComponentCategory.Desk, 1, null),
                new WorkspaceComponentSearchOutcome(
                    "search_similarity_catalogue",
                    WorkspaceComponentCategory.Chair,
                    0,
                    "nothing at or below 250000; the cheapest matching chair is 279000"),
            ]);

        Schemas.Evaluate("catalogue-retrieval.schema.json", retrieval).IsValid.Should().BeTrue();
    }

    [Fact]
    public void A_retrieval_naming_a_component_outside_the_vocabulary_is_rejected()
    {
        Schemas.EvaluateRaw("catalogue-retrieval.schema.json", """
            { "isAvailable": true, "unavailableReason": null,
              "searches": [ { "tool": "search_catalogue", "category": "sofa", "found": 1 } ] }
            """).IsValid.Should().BeFalse(
                "a category outside the seven is not a component this pipeline can compose for");
    }

    [Fact]
    public void A_search_outcome_that_does_not_name_its_tool_is_rejected()
    {
        Schemas.EvaluateRaw("catalogue-retrieval.schema.json", """
            { "isAvailable": true, "unavailableReason": null,
              "searches": [ { "category": "desk", "found": 1 } ] }
            """).IsValid.Should().BeFalse(
                "a component is searched once per tool, so an outcome that does not say which tool it was cannot be read");
    }

    [Fact]
    public void A_search_outcome_naming_a_tool_outside_the_vocabulary_is_rejected()
    {
        Schemas.EvaluateRaw("catalogue-retrieval.schema.json", """
            { "isAvailable": true, "unavailableReason": null,
              "searches": [ { "tool": "search_the_internet", "category": "desk", "found": 1 } ] }
            """).IsValid.Should().BeFalse("only the catalogue's own tools may be reported");
    }

    [Fact] // a component is searched once per tool, so a category legitimately has two entries
    public void A_retrieval_with_both_searches_for_a_component_is_accepted()
    {
        Schemas.EvaluateRaw("catalogue-retrieval.schema.json", """
            { "isAvailable": true, "unavailableReason": null,
              "searches": [
                { "tool": "search_catalogue",            "category": "desk", "found": 4, "reason": null },
                { "tool": "search_similarity_catalogue", "category": "desk", "found": 1, "reason": null } ] }
            """).IsValid.Should().BeTrue();
    }

    [Fact] // the retriever no longer restates catalogue data, which is what the recorded answers replaced
    public void A_retrieval_carrying_products_is_rejected()
    {
        Schemas.EvaluateRaw("catalogue-retrieval.schema.json", """
            { "isAvailable": true, "unavailableReason": null, "searches": [],
              "products": [ { "slot": "Desk", "sku": "X", "name": "n", "amount": 1, "score": null } ] }
            """).IsValid.Should().BeFalse(
                "the products come from the recorded tool answers, so a model restating them is not the contract");
    }

    [Fact]
    public void A_review_serializes_to_something_the_review_schema_accepts()
    {
        var review = new WorkspaceSetupReviewResult(
            IsAcceptable: false,
            Issues: [new WorkspaceSetupReviewIssue("MISSING_SLOT", "the seating is missing")],
            Summary: "One slot is unanswered.");

        Schemas.Evaluate("workspace-setup-review.schema.json", review).IsValid.Should().BeTrue();
    }

    [Fact]
    public void A_review_that_decides_the_next_step_is_rejected()
    {
        // Whether another attempt runs is deterministic code's decision, never the model's, so the contract
        // refuses a verdict that tries to make it.
        Schemas.EvaluateRaw("workspace-setup-review.schema.json", """
            { "isAcceptable": false, "issues": [], "summary": null, "decision": "retry" }
            """).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Every_streamed_event_serializes_to_something_the_event_schema_accepts()
    {
        foreach (var workspaceSuggestionStreamEvent in EveryStreamEvent())
        {
            Schemas.EvaluateRaw(
                "workspace-suggestion-event.schema.json",
                ContractJson.Serialize(workspaceSuggestionStreamEvent)).IsValid
                .Should().BeTrue($"{workspaceSuggestionStreamEvent.GetType().Name} is part of the caller's contract");
        }
    }

    [Fact]
    public void An_event_that_is_not_one_of_the_five_is_rejected()
    {
        Schemas.EvaluateRaw("workspace-suggestion-event.schema.json", """
            { "type": "thinking", "customerWorkflowIdentifier": "run-1" }
            """).IsValid.Should().BeFalse("a caller switches over five event kinds and must not receive a sixth");
    }

    private static IEnumerable<WorkspaceSuggestionStreamEvent> EveryStreamEvent()
    {
        const string workflow = "run-1";
        var setup = new WorkspaceSetupCandidate(
            [new WorkspaceSetupLine(WorkspaceSlot.Desk, "DSKB08XN4JDR", "Sit-Stand Desk", 1, 4_200_000m, "a stable surface")],
            "A calm, focused setup.");

        yield return new WorkspaceProcessingStageStartedEvent(workflow, WorkspaceProcessingStage.VerifyingRequest);
        yield return new WorkspaceProcessingStageCompletedEvent(workflow, WorkspaceProcessingStage.ReviewingWorkspaceSetups);
        yield return new WorkspaceSetupCandidateApprovedEvent(workflow, setup);
        yield return new WorkspaceSetupRetryStartedEvent(workflow, 2, 3);
        yield return new WorkspaceSuggestionRunCompletedEvent(workflow, WorkspaceSuggestionRunStatus.Success, null, 1);
        yield return new WorkspaceSuggestionRunCompletedEvent(workflow, WorkspaceSuggestionRunStatus.Rejected, "not a workspace", 0);
        yield return new WorkspaceSuggestionRunCompletedEvent(workflow, WorkspaceSuggestionRunStatus.Unavailable, "none fitted", 3);
    }
}
