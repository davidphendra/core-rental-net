using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;

namespace CoreRentalNet.Evaluation.Evaluation.Timing;

/// <summary>One tool call's agent transport, guardrail and Host timing.</summary>
public sealed record ToolCallTiming(
    string Tool,
    double DurationMs,
    double GuardrailMs,
    double HostMs,
    IReadOnlyDictionary<string, double> ServerTiming);
