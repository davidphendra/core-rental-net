namespace CoreRentalNet.Host.Infrastructure;

/// <summary>
/// The name of the policy that guards the catalogue API, in one place.
/// </summary>
/// <remarks>
/// A second name for one rule: the pages answer <see cref="CatalogPolicy"/> on the cookie, and a
/// machine caller answers this one on the bearer scheme. Both carry the same requirement, so what
/// entitles a reader of the catalogue is still decided once.
/// </remarks>
internal static class CatalogApiPolicy
{
    public const string Name = "CatalogApiRead";
}
