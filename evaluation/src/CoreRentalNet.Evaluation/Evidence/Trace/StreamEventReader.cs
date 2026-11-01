using System.Text.Json;
using CoreRentalNet.Evaluation.Contracts.Suggestion;
using CoreRentalNet.Evaluation.Contracts.Catalogue;
using CoreRentalNet.Evaluation.Contracts.Reranker;
using CoreRentalNet.Evaluation.Contracts.Trace;

using CoreRentalNet.Evaluation.Evidence.Projection;
using CoreRentalNet.Evaluation.Evidence.Context;
namespace CoreRentalNet.Evaluation.Evidence.Trace;

/// <summary>Reads one typed event object from the agent's response stream.</summary>
public static class StreamEventReader
{
    public static StreamEventDto Read(string json)
        => JsonSerializer.Deserialize<StreamEventDto>(json, EvaluationJson.Options)
           ?? throw new InvalidOperationException($"The stream event was not readable: {json}");
}
