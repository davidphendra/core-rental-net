namespace CoreRentalNet.Agents.Shared.Evaluation;

/// <summary>The first record of a run: which model and prompts produced it.</summary>
internal sealed record EvaluationRunStarted(
    string Model,
    string PromptVersions)
    : EvaluationTraceRecord(null, 0, default);
