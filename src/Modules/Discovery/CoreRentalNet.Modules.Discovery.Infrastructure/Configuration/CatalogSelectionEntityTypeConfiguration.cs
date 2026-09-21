using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreRentalNet.Modules.Discovery.Infrastructure.Configuration;

internal sealed class CatalogSelectionEntityTypeConfiguration : IEntityTypeConfiguration<Selection.CatalogSelection>
{
    public void Configure(EntityTypeBuilder<Selection.CatalogSelection> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Discovery_Selection");
        builder.HasKey(selection => selection.Sku);
        builder.Property(selection => selection.Sku).HasMaxLength(32).ValueGeneratedNever();
        builder.Property(selection => selection.DecayedOffered).IsRequired();
        builder.Property(selection => selection.DecayedChosen).IsRequired();
        builder.Property(selection => selection.LastUpdatedUtc).IsRequired();
    }
}
