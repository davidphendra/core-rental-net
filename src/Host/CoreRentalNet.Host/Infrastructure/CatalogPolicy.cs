namespace CoreRentalNet.Host.Infrastructure;

/// <summary>
/// The name of the policy that guards the catalog, in one place.
/// </summary>
/// <remarks>
/// A name rather than a value: it is how the page and its registration refer to the same rule, so it
/// lives in a single constant rather than as a string written twice and hoping the two agree.
/// </remarks>
internal static class CatalogPolicy
{
    public const string Name = "CatalogRead";
}
