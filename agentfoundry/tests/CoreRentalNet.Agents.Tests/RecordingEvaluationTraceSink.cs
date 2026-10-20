using CoreRentalNet.Agents.Shared.Evaluation;

namespace CoreRentalNet.Agents.Tests;

/// <summary>A trace sink that keeps what the decorator recorded.</summary>
internal sealed class RecordingEvaluationTraceSink : IEvaluationTraceSink
{
    public List<EvaluationStageModelCall> StageCalls { get; } = [];

    public List<EvaluationToolExchange> ToolExchanges { get; } = [];

    public List<string> Lifecycle { get; } = [];

    public void BeginRun(string runId, string model, string promptVersions)
        => Lifecycle.Add($"begin:{runId}:{model}:{promptVersions}");

    public void RecordStageModelCall(EvaluationStageModelCall call) => StageCalls.Add(call);

    public void RecordToolExchange(EvaluationToolExchange exchange) => ToolExchanges.Add(exchange);

    public void CompleteRun(string runStatus) => Lifecycle.Add($"complete:{runStatus}");
}
