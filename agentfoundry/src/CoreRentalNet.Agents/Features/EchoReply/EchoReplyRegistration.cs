using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreRentalNet.Agents.Features.EchoReply;

/// <summary>Everything the echo feature needs to serve its agent, in one place.</summary>
public static class EchoReplyRegistration
{
    /// <summary>Registers the feature when it is enabled, and answers the agent name it serves.</summary>
    public static string? AddEchoReplyFeature(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var echoAgentIdentity = EchoAgentIdentity.FromConfiguration(configuration);

        if (!echoAgentIdentity.IsEnabled)
        {
            return null;
        }

        services.AddSingleton(echoAgentIdentity);
        services.AddSingleton<EchoReplyWorkflowFactory>();
        services.AddSingleton<IEchoReplyWorkflow, EchoReplyWorkflow>();

        // The echo graph holds no run state at all, so it is a singleton like every served agent the root
        // resolution hosting performs can honor.
        services.AddKeyedSingleton<AIAgent>(echoAgentIdentity.AgentName, (provider, _) =>
            provider.GetRequiredService<IEchoReplyWorkflow>().AsAIAgent());

        return echoAgentIdentity.AgentName;
    }
}
