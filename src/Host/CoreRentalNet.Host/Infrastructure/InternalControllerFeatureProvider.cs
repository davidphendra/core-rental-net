using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace CoreRentalNet.Host.Infrastructure;

/// <summary>
/// Discovers controllers that are internal to this application, which the framework's own rule does not.
/// </summary>
/// <remarks>
/// <para>
/// MVC admits a controller only when its type is public. That default is right for the catalogue's endpoint,
/// and the builder's endpoint cannot follow it: a run needs the guard, the agent, the settings and the
/// validator, and those are internal to this application on purpose — so that controller is internal too,
/// because a public type may not name them.
/// </para>
/// <para>
/// <b>The widening is opt-in, and that is the whole of it.</b> An internal type is admitted only when it
/// carries <c>[Controller]</c> — which <c>[ApiController]</c> derives from — so this does not make every
/// internal class whose name ends in "Controller" routable. It makes one declaration, written deliberately,
/// mean what it says. A public controller is still discovered by the rule this one extends, unchanged.
/// </para>
/// <para>
/// <see cref="ControllerFeatureProvider"/> is added beside the framework's own rather than replacing it, so the
/// public rule keeps working exactly as it did and this provider only adds to it.
/// </para>
/// </remarks>
internal sealed class InternalControllerFeatureProvider : ControllerFeatureProvider
{
    protected override bool IsController(TypeInfo typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);

        return base.IsController(typeInfo) || IsMarkedInternalController(typeInfo);
    }

    /// <summary>
    /// An internal type that says, with the attribute, that it is a controller.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every check the framework's own rule makes, except the one about being public — which is replaced by
    /// requiring the attribute, because a name ending in "Controller" is not a declaration and the attribute is.
    /// </para>
    /// <para>
    /// <b>The attribute is looked for on the type itself, and that is load-bearing.</b>
    /// <see cref="ControllerBase"/> carries <c>[Controller]</c>, so an inheriting search finds it on every
    /// subclass — which would admit <em>any</em> internal class deriving from <see cref="ControllerBase"/>, and
    /// make this provider exactly the blanket widening it exists to avoid. Measured: the unmarked probe type
    /// below was admitted until this was changed.
    /// </para>
    /// </remarks>
    private static bool IsMarkedInternalController(TypeInfo typeInfo)
        => typeInfo is { IsClass: true, IsAbstract: false, IsPublic: false }
            && !typeInfo.ContainsGenericParameters
            && !typeInfo.IsDefined(typeof(NonControllerAttribute), inherit: true)
            && typeInfo.IsDefined(typeof(ControllerAttribute), inherit: false);
}
