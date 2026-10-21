using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;

namespace CoreRentalNet.Agents.Shared.Model;

/// <summary>Builds the Foundry project client, with the deployment's own identity.</summary>
internal sealed class FoundryChatClientFactory(
    IConfiguration configuration,
    IHostEnvironment environment,
    ModelTransportRetryOptions transport) : IChatClientFactory
{
    /// <inheritdoc />
    public IChatClient Create()
    {
        var credential = FoundryCredential.Create(environment, configuration);
        return AgentFoundryRegistration.BuildChatClient(configuration, credential, transport);
    }
}
