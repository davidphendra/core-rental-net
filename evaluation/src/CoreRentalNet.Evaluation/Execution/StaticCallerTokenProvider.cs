namespace CoreRentalNet.Evaluation.Execution;

/// <summary>A token supplied by the operator, for a one-off diagnostic run.</summary>
public sealed class StaticCallerTokenProvider(string token) : ICallerTokenProvider
{
    /// <inheritdoc />
    public Task<CallerToken> GetAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(CallerTokenParser.Parse(token));
    }
}
