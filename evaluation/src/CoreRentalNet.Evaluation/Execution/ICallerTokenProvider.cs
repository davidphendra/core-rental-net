namespace CoreRentalNet.Evaluation.Execution;

/// <summary>Supplies a token that is valid now, refreshing it when it is close to expiry.</summary>
public interface ICallerTokenProvider
{
    Task<CallerToken> GetAsync(CancellationToken cancellationToken);
}
