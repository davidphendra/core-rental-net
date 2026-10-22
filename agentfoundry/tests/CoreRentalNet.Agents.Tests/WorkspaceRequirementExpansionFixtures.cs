using System.Text.Json;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
using CoreRentalNet.Agents.Shared.Serialization;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The compact expansion a stage answers with, as JSON, so a test can vary one part of it.</summary>
/// <remarks>
/// The model-facing contract lists only relevant categories. The domain value built here still has all seven,
/// because the mapper fills the omitted ones; that is the boundary these fixtures exercise.
/// </remarks>
internal static class WorkspaceRequirementExpansionFixtures
{
    private const string Query = "a desk and a chair under 500000";

    public const string DeskExpansion =
        """
        { "key": "desk", "retrieval_query": "a wide, stable surface for one screen",
          "search_terms": ["computer desk", "writing desk"], "synonyms": ["workstation"],
          "semantic_concepts": ["a compact home office surface"] }
        """;

    public const string ChairExpansion =
        """
        { "key": "chair", "retrieval_query": "seating for a working day",
          "search_terms": ["office chair"], "synonyms": ["task seating"],
          "semantic_concepts": ["support through a long session"] }
        """;

    public const string MonitorExpansion =
        """
        { "key": "monitor", "retrieval_query": "a screen for a small desk",
          "search_terms": ["monitor"], "synonyms": ["display"],
          "semantic_concepts": ["a screen for one person"] }
        """;

    public static string Json(string? deskExpansion = null, string? monitorExpansion = null)
    {
        var monitorEntry = monitorExpansion is null ? string.Empty : $",\n    {monitorExpansion}";

        return $$"""
            {
              "workspace_intent": {
                "purpose": ["computer work"]
              },
              "total_budget": { "amount": 500000, "currency": "IDR", "is_explicit": true },
              "allocations": [
                { "key": "desk", "amount": 300000 },
                { "key": "chair", "amount": 200000 }
              ],
              "categories": [
                {{deskExpansion ?? DeskExpansion}},
                {{ChairExpansion}}{{monitorEntry}}
              ]
            }
            """;
    }

    public static WorkspaceRequirementExpansionWire Wire() => DeserializeWire(Json());

    public static WorkspaceRequirementExpansion Valid() => Deserialize(Json());

    public static WorkspaceRequirementExpansion Deserialize(string json)
        => WorkspaceRequirementExpansionMapper.Map(DeserializeWire(json), Query);

    private static WorkspaceRequirementExpansionWire DeserializeWire(string json)
        => JsonSerializer.Deserialize<WorkspaceRequirementExpansionWire>(json, ContractJson.Options)
            ?? throw new InvalidOperationException("The fixture is not the contract.");
}
