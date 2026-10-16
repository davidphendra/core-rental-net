using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreRentalNet.Modules.Catalog.Infrastructure.Persistence;

/// <summary>How one catalogue row maps to the <c>Catalog_Product</c> table.</summary>
/// <remarks>
/// The vocabulary columns are typed JSON rather than child tables: nothing queries them at runtime, and
/// the metadata only has to survive the round trip for the ingestion tool, so a column per collection is
/// the honest shape. The index is the one the scoped queries narrow by.
/// </remarks>
internal sealed class CatalogProductConfiguration : IEntityTypeConfiguration<CatalogProduct>
{
    public void Configure(EntityTypeBuilder<CatalogProduct> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Catalog_Product");
        builder.HasKey(product => product.Sku);

        builder.Property(product => product.Sku).HasMaxLength(32).IsRequired();
        builder.Property(product => product.Name).HasMaxLength(400).IsRequired();
        builder.Property(product => product.Category).HasConversion<int>().IsRequired();
        builder.Property(product => product.SubCategory).HasConversion<int>();
        builder.Property(product => product.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(product => product.Currency).HasMaxLength(3).IsRequired();
        builder.Property(product => product.Description).IsRequired();
        builder.Property(product => product.Image).IsRequired();
        builder.Property(product => product.IsFeatured).IsRequired();
        builder.Property(product => product.SortOrder).IsRequired();

        // A category (and, for accessories, a subcategory) is what every runtime query narrows by.
        builder.HasIndex(product => new { product.Category, product.SubCategory });

        builder.PrimitiveCollection(product => product.Tags);
        builder.PrimitiveCollection(product => product.BestFor);
        builder.PrimitiveCollection(product => product.NotFor);

        // Raw JSON text: the attribute bag is only ever carried, never queried, so a converter with a
        // value comparer would add machinery for no reader.
        builder.Property(product => product.AttributesJson).HasColumnName("Attributes").IsRequired();
    }
}
