using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreRentalNet.Modules.Discovery.Infrastructure.Persistence;

/// <summary>How the recipe row maps to the <c>Discovery_ProductVectorRecipe</c> table.</summary>
/// <remarks>
/// <b>Keyless on purpose.</b> The ingestion file declares five <c>NOT NULL</c> columns and no key, so
/// the entity is keyless to reproduce it exactly; a key here would add a constraint the file does not
/// have. The column names are the file's own spelling, so the migration and the file agree.
/// </remarks>
internal sealed class ProductVectorRecipeConfiguration : IEntityTypeConfiguration<ProductVectorRecipe>
{
    public void Configure(EntityTypeBuilder<ProductVectorRecipe> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Discovery_ProductVectorRecipe");
        builder.HasNoKey();

        builder.Property(recipe => recipe.ModelId).HasColumnName("modelId").IsRequired();
        builder.Property(recipe => recipe.Width).HasColumnName("width").IsRequired();
        builder.Property(recipe => recipe.Composition).HasColumnName("composition").IsRequired();
        builder.Property(recipe => recipe.CatalogueHash).HasColumnName("catalogueHash").IsRequired();
        builder.Property(recipe => recipe.CreatedAt).HasColumnName("created_at").IsRequired();
    }
}
