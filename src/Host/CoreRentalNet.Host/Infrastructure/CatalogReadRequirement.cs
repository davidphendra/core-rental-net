using Microsoft.AspNetCore.Authorization;

namespace CoreRentalNet.Host.Infrastructure;

/// <summary>
/// The requirement behind the catalog policy: an account entitled to read the catalog.
/// </summary>
/// <remarks>
/// It carries nothing, because what it means is decided by the handler that answers it, from
/// configuration.
/// </remarks>
internal sealed class CatalogReadRequirement : IAuthorizationRequirement;
