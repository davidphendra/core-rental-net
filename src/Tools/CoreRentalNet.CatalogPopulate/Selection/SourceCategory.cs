namespace CoreRentalNet.CatalogPopulate.Selection;

/// <summary>One catalogue category: how a source row is recognised, named, and how many are extracted.</summary>
internal sealed record SourceCategory(
    string Key,
    string Prefix,
    string Category,
    string? SubCategory,
    string MetaFileKey,
    string ReviewFileKey,
    int TargetTotal);
