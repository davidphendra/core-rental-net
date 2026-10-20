using System.Text.Json;
using CoreRentalNet.Evaluation.Contracts;

namespace CoreRentalNet.Evaluation.Evidence;

/// <summary>Reads one typed event object from the agent's response stream.</summary>
public static class StreamEventReader
{
    public static StreamEventDto Read(string json)
        => JsonSerializer.Deserialize<StreamEventDto>(json, EvaluationJson.Options)
           ?? throw new InvalidOperationException($"The stream event was not readable: {json}");
}
