using CoreRentalNet.Evaluation.Execution.Auth;
using CoreRentalNet.Evaluation.Execution.Tools;
using CoreRentalNet.Evaluation.Execution.Running;

namespace CoreRentalNet.Evaluation.Execution.Transport;

/// <summary>Runs one request with a token that is valid now, and yields each typed event object.</summary>
public interface IAgentClient
{
    IAsyncEnumerable<string> RunAsync(
        string requestJson,
        string callerToken,
        CancellationToken cancellationToken);
}
