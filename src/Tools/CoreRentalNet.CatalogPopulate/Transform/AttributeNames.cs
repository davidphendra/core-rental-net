namespace CoreRentalNet.CatalogPopulate.Transform;

/// <summary>The attribute keys the catalogue's metadata carries, lower-case and unspaced.</summary>
internal static class AttributeNames
{
    public const string Brand = "brand";
    public const string Rating = "rating";
    public const string RatingCount = "ratingcount";
    public const string PriceUsd = "priceusd";
    public const string PriceIdrPerMonth = "priceidrpermonth";
    public const string Dimensions = "dimensions";
    public const string Material = "material";
    public const string Color = "color";
    public const string Weight = "weight";
    public const string CategoryPath = "categorypath";
    public const string Type = "type";
    public const string DeskType = "desktype";
    public const string AssemblyRequired = "assemblyrequired";
    public const string ReviewCount = "reviewcount";
    public const string ReviewSummary = "reviewsummary";

    /// <summary>The value a key carries when the source offered none.</summary>
    public const string Unspecified = "unspecified";
}
