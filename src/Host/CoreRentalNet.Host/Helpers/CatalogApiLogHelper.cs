namespace CoreRentalNet.Host.Helpers;

/// <summary>
/// One line per catalogue request, so a machine caller's use of the endpoint is visible.
/// </summary>
/// <remarks>
/// Written only after the request has been authorised and answered, so a refused call is never
/// recorded as a read. The filters are the values as asked for and the count is what came back; the
/// response body is not logged.
/// </remarks>
internal static class CatalogApiLogHelper
{
    /// <summary>The logger category the line is written under, so a test can find it.</summary>
    public const string Category = "CoreRentalNet.Host.Controllers.CatalogApiLogHelper";

    public static void Called(ILogger logger, string caller, string? category, string? subCategory, string? search, int count)
        => logger.LogInformation(
            "Catalog read by {Caller} (category={Category}, subCategory={SubCategory}, search={Search}) returned {Count} products.",
            caller,
            category,
            subCategory,
            search,
            count);
}
