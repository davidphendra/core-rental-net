using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CoreRentalNet.Agents.Shared.Scoping;

/// <summary>The current request's scope, which is the run's scope.</summary>
/// <remarks>
/// <b>It refuses rather than falling back to the root.</b> A run that reaches here outside a request has no
/// caller and no request scope, and resolving from the root instead would hand it the process's own instance of
/// a service the run is supposed to own — the exact captive state this type exists to prevent.
/// </remarks>
internal sealed class RunScope(IHttpContextAccessor httpContextAccessor) : IRunScope
{
    public T Resolve<T>() where T : notnull
        => RequestServices().GetRequiredService<T>();

    public bool TryResolve<T>(out T service) where T : notnull
    {
        if (httpContextAccessor.HttpContext?.RequestServices is not { } requestServices)
        {
            service = default!;
            return false;
        }

        service = requestServices.GetRequiredService<T>();
        return true;
    }

    private IServiceProvider RequestServices()
        => httpContextAccessor.HttpContext?.RequestServices
            ?? throw new InvalidOperationException(
                "A run reached for its run-scoped services outside an HTTP request.");
}
