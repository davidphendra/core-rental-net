using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace CoreRentalNet.Agents.Features.EchoReverse;

public static class EchoReverseRegistration
{
    /// <remarks>
    /// The identity is handed in rather than read here: the composition root reads configuration once, so the
    /// name this agent is registered under and the name the startup report prints come from one value. The agent
    /// is transient because the echo workflow has the same single-owner rule as every workflow: a registration
    /// that reused it would make a second concurrent request fail to take workflow ownership.
    /// </remarks>
    public static string? AddEchoReverseFeature(this IServiceCollection services, EchoAgentIdentity echoAgentIdentity)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(echoAgentIdentity);

        if (!echoAgentIdentity.IsEnabled)
        {
            return null;
        }

        services.AddKeyedTransient<AIAgent>(
            echoAgentIdentity.AgentName,
            (_, _) => EchoWorkflowFactory.Build()
                .AsAIAgent(
                    name: echoAgentIdentity.AgentName,
                    includeWorkflowOutputsInResponse: true));

        return echoAgentIdentity.AgentName;
    }
}
