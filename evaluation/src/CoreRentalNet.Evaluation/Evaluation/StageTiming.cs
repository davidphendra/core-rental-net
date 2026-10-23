namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>One workflow node's measured wall time and its measured children.</summary>
public sealed record StageTiming(
    string Stage,
    double TotalMs,
    double ModelMs,
    double ToolMs,
    double GuardrailMs,
    double HostMs,
    double UnattributedMs)
{
    /// <summary>Model time spent in the tool-planning call.</summary>
    public double ToolPlanningModelMs { get; init; }

    /// <summary>Model time spent in the structured answer after tool results.</summary>
    public double FinalStructuredModelMs { get; init; }

    /// <summary>How many model calls this node made.</summary>
    public int ModelCallCount { get; init; }

    /// <summary>How many tool calls this node made.</summary>
    public int ToolCallCount { get; init; }
}
