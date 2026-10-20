namespace CoreRentalNet.Evaluation.Evidence;

/// <summary>The workspace-specific evidence projected from a raw trace.</summary>
public sealed record WorkspaceEvidence(
    IReadOnlyList<ToolAnswerEvidence> ToolAnswers,
    IReadOnlyList<RerankSelectionEvidence> RerankSelections);
