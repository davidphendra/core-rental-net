using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace CoreRentalNet.Agents.Features.EchoReverse;

public static class EchoReverseRegistration
{
    /// <remarks>
    /// The identity is handed in rather than read here: the composition root reads configuration once, so the
    /// name this agent is registered under and the name the startup report prints come from one value.
    /// </remarks>
    public static string? AddEchoReverseFeature(this IServiceCollection services, EchoAgentIdentity echoAgentIdentity)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(echoAgentIdentity);

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
