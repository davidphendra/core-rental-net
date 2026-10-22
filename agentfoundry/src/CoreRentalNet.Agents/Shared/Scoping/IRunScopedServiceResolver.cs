namespace CoreRentalNet.Agents.Shared.Scoping;

/// <summary>Reaches the services that belong to one run, from a service that outlives it.</summary>
/// <remarks>
/// The hosted agent and its graph are created per request, but the run-scoped services they use still belong to
/// that request. The graph reaches them through the request rather than capturing them, so the stateless factory,
/// builder and guard registrations cannot create captive state.
/// </remarks>
internal interface IRunScopedServiceResolver
{
    /// <summary>The run's own instance of a run-scoped service, or a failure when there is no run.</summary>
    T Resolve<T>() where T : notnull;

    /// <summary>The run's own instance, or false when there is no run to take it from.</summary>
    /// <remarks>
    /// For a caller that only passes a service through and has a correct answer without one — a decorator's
    /// <c>GetService</c>, which the framework also calls while the graph is being built and no request exists yet.
    /// </remarks>
    bool TryResolve<T>(out T service) where T : notnull;
}
