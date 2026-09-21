using CoreRentalNet.Host.Agents;
using CoreRentalNet.Host.AiBuilder;

namespace CoreRentalNet.Host.Tests;

/// <summary>Run budgets, for the tests that are about something other than the deadline.</summary>
/// <remarks>
/// A test that passes <c>CancellationToken.None</c> to the agent now has to say what the run's deadline is,
/// because the port carries both tokens. These keep that from being five slightly different expressions.
/// </remarks>
internal static class RunBudgets
{
    /// <summary>A run with the shipped deadline and a customer who is still connected.</summary>
    public static RunBudget Open(CancellationToken customer = default)
        => RunBudget.Over(customer, SuggestionAgentSettings.DefaultTimeoutSeconds);

    /// <summary>A run the customer has stopped: their token is signalled and the deadline never was.</summary>
    /// <remarks>
    /// The distinction the class exists for. A run that reports "stopped" needs a customer token that really is
    /// cancelled, because that is the only thing that tells a stop from a timeout - and a fake that threw a
    /// cancellation without cancelling the token would be reporting the wrong ending and passing.
    /// </remarks>
    public static RunBudget StoppedByCustomer()
        => RunBudget.Over(new CancellationToken(canceled: true), SuggestionAgentSettings.DefaultTimeoutSeconds);

    /// <summary>A run whose deadline is already spent, for the tests about what a timeout means.</summary>
    /// <remarks>
    /// A zero timeout rather than a cancelled customer token: the difference between the two is the whole point
    /// of <see cref="RunBudget"/>, and a test that got it wrong would be testing the other ending.
    /// </remarks>
    public static RunBudget Spent() => RunBudget.Over(CancellationToken.None, 0);
}
