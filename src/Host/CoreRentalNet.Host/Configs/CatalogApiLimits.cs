namespace CoreRentalNet.Host.Configs;

/// <summary>
/// How many products one answer may carry, and what a caller gets when it does not say.
/// </summary>
/// <remarks>
/// Above the catalogue's present size, so a request that names no limit is complete and says so. The
/// cap is not paging and not a result cap on the catalogue: it is what stops growth from turning into
/// a silently partial answer, which is why the envelope reports how many products matched beside how
/// many it carried, and whether those two numbers differ.
/// </remarks>
public static class CatalogApiLimits
{
    /// <summary>The rows an answer carries when the caller names no limit.</summary>
    public const int Default = 500;
}
