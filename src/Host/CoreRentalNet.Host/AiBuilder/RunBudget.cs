namespace CoreRentalNet.Host.AiBuilder;

/// <summary>
/// How long one run has, and the two different endings a cancellation can mean.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two tokens, because one cannot say which ending happened.</b> A run can be cancelled because the customer
/// stopped it — or closed the tab, which is the same signal — or because it ran out of time. The first is a
/// neutral end the page reports as stopped; the second is a failure the customer retries, on a run that has
/// already spent money. Both arrive as <see cref="OperationCanceledException"/>, and the only thing that tells
/// them apart is which token was signalled.
/// </para>
/// <para>
/// <b>They are held together rather than passed as two arguments, and that is not tidiness.</b> Two adjacent
/// <c>CancellationToken</c> parameters of the same type are the easiest thing in C# to swap, and the compiler
/// would not notice: the swap would report every timeout as a customer stop and every stop as a timeout, and
/// neither would fail a test that only looked at the outcome's name.
/// </para>
/// <para>
/// The run's token is linked to the customer's, so either cancels the work; the budget only ADDS a deadline.
/// The timeout comes from the agent settings, and ADR 0002's reason for it stands: it is deliberately above the
/// p95 target, so a slow-but-correct run finishes rather than being cut off at the number it was allowed to
/// reach.
/// </para>
/// </remarks>
internal sealed class RunBudget : IDisposable
{
    private readonly CancellationTokenSource _run;

    private RunBudget(CancellationToken customer, int timeoutSeconds)
    {
        Customer = customer;
        _run = CancellationTokenSource.CreateLinkedTokenSource(customer);
        _run.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
    }

    /// <summary>The customer's own token: signalled when they stop or their connection goes away.</summary>
    public CancellationToken Customer { get; }

    /// <summary>The run's token: the customer's, plus this run's deadline. Everything the run does uses this.</summary>
    public CancellationToken Run => _run.Token;

    /// <summary>A budget over the customer's token.</summary>
    public static RunBudget Over(CancellationToken customer, int timeoutSeconds)
        => new(customer, timeoutSeconds);

    /// <summary>True when the run's own deadline is what ended it.</summary>
    /// <remarks>
    /// The distinction the class exists for, in one place, so neither the retrieval nor the agent has to work it
    /// out again: the budget is spent and the customer is still connected.
    /// </remarks>
    public bool Spent => Run.IsCancellationRequested && !Customer.IsCancellationRequested;

    public void Dispose() => _run.Dispose();
}
