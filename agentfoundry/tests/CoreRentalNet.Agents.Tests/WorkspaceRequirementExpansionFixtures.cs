using System.Text.Json;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
using CoreRentalNet.Agents.Shared.Serialization;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The expansion a stage answers with, as JSON, so a test can vary one part of it.</summary>
/// <remarks>
/// Every category is present because the contract requires all seven, and the six a test does not care about are
/// written once here rather than in each test. The desk is the one tests vary, because it is the one the desk
/// budget and the desk words are about.
/// </remarks>
internal static class WorkspaceRequirementExpansionFixtures
{
    /// <summary>One desk requirement, and the part of the document a test replaces when it needs another.</summary>
    public const string DeskExpansion =
        """
        { "relevant": true, "retrieval_query": "a wide, stable surface for one screen",
          "search_terms": ["computer desk", "writing desk"], "synonyms": ["workstation"],
          "semantic_concepts": ["a compact home office surface"],
          "budget": { "max_amount": 300000, "currency": "IDR", "is_explicit": false, "is_derived": true } }
        """;

    /// <summary>The desk requirement for a category the customer did not ask for.</summary>
    public const string DeskNotRequested =
        """
        { "relevant": false, "retrieval_query": "", "search_terms": [], "synonyms": [],
          "semantic_concepts": [],
          "budget": { "max_amount": null, "currency": null, "is_explicit": false, "is_derived": false } }
        """;

    /// <summary>The whole document, with the desk requirement replaced when one is supplied.</summary>
    public static string Json(string? deskExpansion = null) => $$"""
        {
          "original_query": "a desk and a chair under 500000",
          "workspace_intent": {
            "purpose": ["computer work"],
            "style": [],
            "experience": [],
            "usage": []
          },
          "total_budget": { "amount": 500000, "currency": "IDR", "is_explicit": true },
          "categories": {
            "desk": {{deskExpansion ?? DeskExpansion}},
            "chair": { "relevant": true, "retrieval_query": "seating for a working day",
                       "search_terms": ["office chair"], "synonyms": ["task seating"],
                       "semantic_concepts": ["support through a long session"],
                       "budget": { "max_amount": 200000, "currency": "IDR", "is_explicit": false, "is_derived": true } },
            "monitor": { "relevant": false, "retrieval_query": "", "search_terms": [], "synonyms": [],
                         "semantic_concepts": [],
                         "budget": { "max_amount": null, "currency": null, "is_explicit": false, "is_derived": false } },
            "lamp": { "relevant": false, "retrieval_query": "", "search_terms": [], "synonyms": [],
                      "semantic_concepts": [],
                      "budget": { "max_amount": null, "currency": null, "is_explicit": false, "is_derived": false } },
            "plant": { "relevant": false, "retrieval_query": "", "search_terms": [], "synonyms": [],
                       "semantic_concepts": [],
                       "budget": { "max_amount": null, "currency": null, "is_explicit": false, "is_derived": false } },
            "bean_bag": { "relevant": false, "retrieval_query": "", "search_terms": [], "synonyms": [],
                          "semantic_concepts": [],
                          "budget": { "max_amount": null, "currency": null, "is_explicit": false, "is_derived": false } },
            "coffee_machine": { "relevant": false, "retrieval_query": "", "search_terms": [], "synonyms": [],
                                "semantic_concepts": [],
                                "budget": { "max_amount": null, "currency": null, "is_explicit": false, "is_derived": false } }
          }
        }
        """;

    /// <summary>The document as a value, for tests that assert what a stage kept rather than what it was given.</summary>
    public static WorkspaceRequirementExpansion Valid() => Deserialize(Json());

    public static WorkspaceRequirementExpansion Deserialize(string json)
        => JsonSerializer.Deserialize<WorkspaceRequirementExpansion>(json, ContractJson.Options)
            ?? throw new InvalidOperationException("The fixture is not the contract.");

}
