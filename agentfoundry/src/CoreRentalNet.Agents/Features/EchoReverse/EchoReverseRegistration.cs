using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace CoreRentalNet.Agents.Features.EchoReverse;

public static class EchoReverseRegistration
{
    public static string? AddEchoReverseFeature(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var echoAgentIdentity = EchoAgentIdentity.FromConfiguration(configuration);
        if (!echoAgentIdentity.IsEnabled)
        {
            return null;
        }

        services.AddKeyedSingleton<AIAgent>(
            echoAgentIdentity.AgentName,
            EchoWorkflowFactory.Build()
                                    .AsAIAgent(name: echoAgentIdentity.AgentName)
        );

        return echoAgentIdentity.AgentName;
    }
}
