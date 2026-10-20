namespace CoreRentalNet.Agents.Shared.Evaluation;

/// <summary>One function call a model asked for, as the call was handed to the framework.</summary>
internal sealed record EvaluationToolCall(
    string Name,
    IReadOnlyDictionary<string, object?> Arguments);
