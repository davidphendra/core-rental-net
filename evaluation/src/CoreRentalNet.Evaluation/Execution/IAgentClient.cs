namespace CoreRentalNet.Evaluation.Execution;

/// <summary>Runs one request and yields each typed event object.</summary>
public interface IAgentClient
{
    IAsyncEnumerable<string> RunAsync(string requestJson, CancellationToken cancellationToken);
}
