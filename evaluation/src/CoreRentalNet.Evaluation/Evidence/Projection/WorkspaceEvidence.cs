using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Context;

namespace CoreRentalNet.Evaluation.Evidence.Projection;

/// <summary>The workspace-specific evidence projected from a raw trace.</summary>
public sealed record WorkspaceEvidence(
    IReadOnlyList<ToolAnswerEvidence> ToolAnswers,
    IReadOnlyList<RerankSelectionEvidence> RerankSelections);
