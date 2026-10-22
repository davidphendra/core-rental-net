namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>One workflow node's measured wall time and its measured children.</summary>
public sealed record StageTiming(
    string Stage,
    double TotalMs,
    double ModelMs,
    double ToolMs,
    double GuardrailMs,
    double HostMs,
    double UnattributedMs);
