using System.Runtime.CompilerServices;

namespace CoreRentalNet.Host.Agents;

/// <summary>What is registered when the feature is off or the endpoint is missing.</summary>
/// <remarks>
/// It yields <b>unavailable</b>, never a suggestion and never a refusal — the deployment has not been told
/// where the agent is, which is not the customer's problem. The AI section is hidden in this state; this
/// implementation exists so that nothing downstream has to check whether the feature is on.
/// </remarks>
internal sealed class NoAgentConfigured : ISuggestionAgent
{
    public async IAsyncEnumerable<AgentSuggestionEvent> StreamAsync(
        SuggestionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        yield return new AgentSuggestionEvent.Unavailable("No suggestion agent is configured.");
    }
}
