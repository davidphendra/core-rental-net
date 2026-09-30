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
        services.AddScoped<EchoReplyWorkflowFactory>();
        services.AddScoped<IEchoReplyWorkflow, EchoReplyWorkflow>();

        services.AddKeyedScoped<AIAgent>(echoAgentIdentity.AgentName, (provider, _) =>
            provider.GetRequiredService<IEchoReplyWorkflow>().AsAIAgent());

        return echoAgentIdentity.AgentName;
    }
}
