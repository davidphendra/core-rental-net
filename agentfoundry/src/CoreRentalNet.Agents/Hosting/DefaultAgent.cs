using Microsoft.Agents.AI;

namespace CoreRentalNet.Agents.Hosting;

/// <summary>Registers the agent a request that carries no name is answered by.</summary>
/// <remarks>
/// Hosting falls back to a non-keyed <see cref="AIAgent"/> when the request names none — exactly what
/// <c>azd ai agent invoke --local</c> sends — so the fallback needs a target. It is transient because the keyed
/// agent it resolves is transient: each request needs its own workflow owner, and a singleton here would cache
/// the first keyed agent forever. Transient is still root-resolvable, which is what hosting uses; a scoped
/// registration would be rejected by scope validation.
/// </remarks>
internal static class DefaultAgent
{
    /// <summary>Points the default <see cref="AIAgent"/> at the keyed registration for the given name.</summary>
    public static IServiceCollection AddDefaultAgent(this IServiceCollection services, string defaultAgentName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultAgentName);

        services.AddTransient<AIAgent>(serviceProvider =>
            serviceProvider.GetRequiredKeyedService<AIAgent>(defaultAgentName));

        return services;
    }
}
