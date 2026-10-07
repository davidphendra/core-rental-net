using CoreRentalNet.Agents.Shared.Scoping;

namespace CoreRentalNet.Agents.Tests;

/// <summary>A run scope whose services are the test's own, so a graph can be built and run without a host.</summary>
internal sealed class StubRunScope : IRunScope
{
    private readonly Dictionary<Type, object> _services = [];

    public StubRunScope(params (Type Service, object Instance)[] services)
    {
        foreach (var (service, instance) in services)
        {
            _services[service] = instance;
        }
    }

    /// <summary>Adds one more service, for a graph that has to name itself as a service to itself.</summary>
    public StubRunScope With<T>(T instance) where T : notnull
    {
        _services[typeof(T)] = instance;

        return this;
    }

    public T Resolve<T>() where T : notnull
        => TryResolve<T>(out var service)
            ? service
            : throw new InvalidOperationException($"No run-scoped {typeof(T).Name} was provided.");

    public bool TryResolve<T>(out T service) where T : notnull
    {
        if (_services.TryGetValue(typeof(T), out var instance) && instance is T typed)
        {
            service = typed;
            return true;
        }

        service = default!;
        return false;
    }
}
