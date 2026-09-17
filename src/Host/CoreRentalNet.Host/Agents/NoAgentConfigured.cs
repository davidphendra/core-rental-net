using CoreRentalNet.Modules.Workspace.Application.Contracts.Suggestion;

namespace CoreRentalNet.Host.Agents;

/// <summary>
/// The agent a deployment that has not configured one has: none, said out loud.
/// </summary>
/// <remarks>
/// A null object rather than a missing registration, because the operation still exists: a customer can
/// ask for a suggestion and must be told the service is unavailable, which is a different answer from a
/// page that cannot be reached or a request that is wrong. It also means the composition validates -
/// every port has an implementation - and that a deployment without an agent fails at the one thing it
/// is missing rather than at start-up with a message about a service descriptor.
/// </remarks>
internal sealed class NoAgentConfigured : IAgentSuggestions
{
    /// <inheritdoc />
    public bool IsConfigured => false;

    /// <summary>Nothing, because nothing will ask: the port reports itself unconfigured first.</summary>
    public async IAsyncEnumerable<AgentSuggestionMessage> AskAsync(
        string query,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        yield break;
    }
}
