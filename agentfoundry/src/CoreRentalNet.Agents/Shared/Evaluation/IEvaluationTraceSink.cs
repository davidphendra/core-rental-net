namespace CoreRentalNet.Agents.Shared.Evaluation;

/// <summary>Persists raw evidence from one run. It knows nothing about catalogues or workspaces.</summary>
/// <remarks>
/// An observer, not a guard: every method returns nothing and cannot change what the run does. A deployment that
/// does not configure a trace path never constructs an implementation, so the observation has no runtime cost.
/// </remarks>
internal interface IEvaluationTraceSink
{
    /// <summary>Opens the record for one run.</summary>
    void BeginRun(string runId, string model, string promptVersions);

    /// <summary>Records one stage's model call and raw answer.</summary>
    void RecordStageModelCall(EvaluationStageModelCall call);

    /// <summary>Records one tool call and the answer the model saw.</summary>
    void RecordToolExchange(EvaluationToolExchange exchange);

    /// <summary>Closes the record for one run.</summary>
    void CompleteRun(string runStatus);
}
