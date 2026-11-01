using CoreRentalNet.Evaluation.Execution.Transport;
using CoreRentalNet.Evaluation.Execution.Tools;
using CoreRentalNet.Evaluation.Execution.Running;

namespace CoreRentalNet.Evaluation.Execution.Auth;

/// <summary>Supplies a token that is valid now, refreshing it when it is close to expiry.</summary>
public interface ICallerTokenProvider
{
    Task<CallerToken> GetAsync(CancellationToken cancellationToken);
}
