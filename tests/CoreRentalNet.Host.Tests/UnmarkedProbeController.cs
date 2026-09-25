using Microsoft.AspNetCore.Mvc;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// An internal class that names itself a controller and says nothing else.
/// </summary>
/// <remarks>
/// This type exists only to be the negative half of <see cref="InternalControllerFeatureProviderTests"/>: it is
/// the case where the name is not enough. It lives in the test assembly, which is not an application part of the
/// application, so it can never be routed however a discovery rule is written.
/// </remarks>
internal sealed class UnmarkedProbeController : ControllerBase;
