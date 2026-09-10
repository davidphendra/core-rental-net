using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Catalog.Domain;

/// <summary>
/// A rentable item. Immutable: the catalog is a read-only source and nothing in this
/// application may add, update or delete a product.
/// </summary>
public sealed record Product
{
    public Product(
        Sku sku,
        string name,
        ProductCategory category,
        ProductSubCategory? subCategory,
        Money monthlyPrice,
        string description,
        string imagePath,
        ProductBadge? badge,
        bool imageAvailable)
    {
        ArgumentNullException.ThrowIfNull(sku);

        Sku = sku;
        Name = Guard.NotEmpty(name, "Product name", 120);
        Category = category;
        SubCategory = ResolveSubCategory(category, subCategory, Sku);
        MonthlyPrice = monthlyPrice ?? throw new DomainRuleViolationException("A product requires a monthly price.");
        Description = Guard.NotEmpty(description, "Product description", 400);
        ImagePath = Guard.NotEmpty(imagePath, "Product image path", 300);
        Badge = badge;
        ImageAvailable = imageAvailable;
    }

    public Sku Sku { get; }

    public string Name { get; }

    public ProductCategory Category { get; }

    public ProductSubCategory? SubCategory { get; }

    public Money MonthlyPrice { get; }

    public string Description { get; }

    public string ImagePath { get; }

    public ProductBadge? Badge { get; }

    /// <summary>False when the referenced image file is not present, so the UI renders a placeholder.</summary>
    public bool ImageAvailable { get; }

    public bool IsFeatured => Badge == ProductBadge.Popular;

    private static ProductSubCategory? ResolveSubCategory(
        ProductCategory category,
        ProductSubCategory? subCategory,
        Sku sku)
    {
        if (category == ProductCategory.Accessory && subCategory is null)
        {
            throw new DomainRuleViolationException($"Accessory '{sku}' requires a subcategory.");
        }

        if (category != ProductCategory.Accessory && subCategory is not null)
        {
            throw new DomainRuleViolationException(
                $"'{sku}' is a {category} and cannot have the subcategory '{subCategory}'.");
        }

        return subCategory;
    }
}
