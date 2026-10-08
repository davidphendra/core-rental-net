using Microsoft.Agents.AI;

namespace CoreRentalNet.Agents.Hosting;

/// <summary>Registers the agent a request that carries no name is answered by.</summary>
/// <remarks>
/// Hosting falls back to a non-keyed <see cref="AIAgent"/> when the request names none — exactly what
/// <c>azd ai agent invoke --local</c> sends — so the fallback needs a target. It is a singleton for the same
/// reason the keyed agents are: hosting resolves it from the root container on every request, not only a nameless
/// one, so a scoped registration is rejected by scope validation.
/// </remarks>
internal static class DefaultAgent
{
    /// <summary>Points the default <see cref="AIAgent"/> at the keyed registration for the given name.</summary>
    public static IServiceCollection AddDefaultAgent(this IServiceCollection services, string defaultAgentName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultAgentName);

        services.AddSingleton<AIAgent>(serviceProvider =>
            serviceProvider.GetRequiredKeyedService<AIAgent>(defaultAgentName));

        return services;
    }
}
