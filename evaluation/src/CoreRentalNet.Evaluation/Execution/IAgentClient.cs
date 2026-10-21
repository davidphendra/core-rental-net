namespace CoreRentalNet.Evaluation.Execution;

/// <summary>Runs one request with a token that is valid now, and yields each typed event object.</summary>
public interface IAgentClient
{
    IAsyncEnumerable<string> RunAsync(
        string requestJson,
        string callerToken,
        CancellationToken cancellationToken);
}
