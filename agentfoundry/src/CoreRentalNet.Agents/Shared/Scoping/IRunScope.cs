namespace CoreRentalNet.Agents.Shared.Scoping;

/// <summary>Reaches the services that belong to one run, from a service that outlives it.</summary>
/// <remarks>
/// The hosting resolves a served agent from the root container, so the graph it serves outlives the request.
/// What a run owns is scoped, so the graph reaches it through the request rather than holding it. Microsoft's
/// hosting is expected to resolve the keyed agent from <c>HttpContext.RequestServices</c> per request; until
/// that ships this is the same scope, reached one hop later.
/// </remarks>
internal interface IRunScope
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
