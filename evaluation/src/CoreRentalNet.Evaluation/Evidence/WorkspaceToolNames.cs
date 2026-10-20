namespace CoreRentalNet.Evaluation.Evidence;

/// <summary>The catalogue tool identities the evaluator pins against the application's MCP surface.</summary>
public static class WorkspaceToolNames
{
    public const string NameSearch = "search_catalogue";
    public const string MeaningSearch = "search_similarity_catalogue";

    public static IReadOnlyList<string> All { get; } = [NameSearch, MeaningSearch];
}
