using Microsoft.Extensions.DependencyInjection;

namespace CoreRentalNet.Agents.Shared.Evaluation;

/// <summary>Replaces one registered service with a decorator, without the service knowing about it.</summary>
/// <remarks>
/// The decorator is applied at the composition root, after the feature has registered its own implementation.
/// The original descriptor's lifetime is preserved; a scoped service stays scoped.
/// </remarks>
internal static class ServiceCollectionDecoration
{
    public static IServiceCollection Decorate<TService>(
        this IServiceCollection services,
        Func<TService, IServiceProvider, TService> decorate)
        where TService : class
    {
        var descriptor = services.LastOrDefault(service => service.ServiceType == typeof(TService))
            ?? throw new InvalidOperationException(
                $"{typeof(TService).Name} must be registered before it is decorated.");

        services.Remove(descriptor);

        services.Add(new ServiceDescriptor(
            typeof(TService),
            provider => decorate((TService)Create(descriptor, provider), provider),
            descriptor.Lifetime));

        return services;
    }

    private static object Create(ServiceDescriptor descriptor, IServiceProvider provider)
        => descriptor.ImplementationInstance
           ?? descriptor.ImplementationFactory?.Invoke(provider)
           ?? ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType!);
}
